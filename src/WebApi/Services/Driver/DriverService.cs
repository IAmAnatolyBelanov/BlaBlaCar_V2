using FluentValidation;
using Newtonsoft.Json;
using Polly;
using System.Collections.Concurrent;
using System.Text;
using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Models.DriverServiceModels;
using WebApi.Repositories;
using WebApi.Services.Validators;

namespace WebApi.Services.Driver;

public class DriverServiceValidationCodes : ValidationCodes
{
	public const string VinContainsBannedSymbols = "DriverService_VinContainsBannedSymbols";
}

public interface IDriverService
{
	Task<DriverData> ValidateDriverLicense(Guid userId, Models.DriverServiceModels.Driver driver, CancellationToken ct);
	Task<PersonData> ValidatePerson(IPostgresSession session, Person person, CancellationToken ct);
	Task<CarDto> SearchCar(string vinCode, CancellationToken ct);
}

public class DriverService : IDriverService
{
	private readonly IDriverServiceConfig _config;
	private readonly IValidator<Person> _personValidator;
	private readonly IPersonMapper _personMapper;
	private readonly IClock _clock;
	private readonly IValidator<Models.DriverServiceModels.Driver> _driverValidator;
	private readonly IGibddServiceDrivingLicenseResponseMapper _gibddMapper;


	private readonly ILogger _logger = Log.ForContext<DriverService>();
	private readonly HttpClient _httpClient = new(new SocketsHttpHandler
	{
		PooledConnectionLifetime = TimeSpan.FromMinutes(15),
	});

	private readonly string _taxServiceBaseUrl;
	private readonly string _mvdServiceBaseUrl;
	private readonly string _gibddServiceBaseUrl;
	private readonly string _vinConverterServiceBaseUrl;
	private readonly string _vinDecoderServiceBaseUrl;
	private readonly IAsyncPolicy _asyncPolicy;
	private readonly ISessionFactory _sessionFactory;
	private readonly ICloudApiResponseInfoRepository _cloudApiResponseInfoRepository;
	private readonly IPersonDataRepository _personDataRepository;
	private readonly IDriverDataRepository _driverDataRepository;
	private readonly ICarRepository _carRepository;
	private readonly ICarMapper _carMapper;
	private readonly IVinValidator _vinValidator;

	private readonly ConcurrentQueue<CloudApiResponseInfo> _cloudApiResponsesForSave = new();
	private readonly Task _cloudApiResponsesSaver;

	public DriverService(
		IDriverServiceConfig driverServiceConfig,
		IValidator<Person> personValidator,
		IPersonMapper personMapper,
		IClock clock,
		IValidator<Models.DriverServiceModels.Driver> driverValidator,
		IGibddServiceDrivingLicenseResponseMapper gibddMapper,
		ISessionFactory sessionFactory,
		ICloudApiResponseInfoRepository cloudApiResponseInfoRepository,
		IPersonDataRepository personDataRepository,
		IDriverDataRepository driverDataRepository,
		ICarRepository carRepository,
		ICarMapper carMapper,
		IVinValidator vinValidator)
	{
		_config = driverServiceConfig;
		_personValidator = personValidator;
		_personMapper = personMapper;
		_clock = clock;
		_driverValidator = driverValidator;
		_gibddMapper = gibddMapper;
		_sessionFactory = sessionFactory;
		_cloudApiResponseInfoRepository = cloudApiResponseInfoRepository;
		_personDataRepository = personDataRepository;
		_driverDataRepository = driverDataRepository;
		_carRepository = carRepository;
		_carMapper = carMapper;
		_vinValidator = vinValidator;

		var baseUrl = new Uri(_config.ApiCloudBaseUrl);

		_taxServiceBaseUrl = new Uri(baseUrl, "nalog.php").AbsoluteUri;
		_mvdServiceBaseUrl = new Uri(baseUrl, "mvd.php").AbsoluteUri;
		_gibddServiceBaseUrl = new Uri(baseUrl, "gibdd.php").AbsoluteUri;
		_vinConverterServiceBaseUrl = new Uri(baseUrl, "converter.php").AbsoluteUri;
		_vinDecoderServiceBaseUrl = new Uri(baseUrl, "vindecoder.php").AbsoluteUri;

		_asyncPolicy = Policy
			.Handle<HttpRequestException>()
			.WaitAndRetryAsync(_config.RetryCount,
			attempt => _config.RetryDelay,
			(exception, timeSpan, attempt, context) =>
			{
				_logger.Error("Failed to fetch api-cloud response. Attempt {Attempt}, time delay {TimeDelay}, context {Context}, exception {Exception}",
					attempt, timeSpan, context, exception);
			});

		_cloudApiResponsesSaver = StartSaverCloudApiResponseBackgroundTask();

	}

	public async Task<CarDto> SearchCar(string vinCode, CancellationToken ct)
	{
		var start = _clock.Now;
		vinCode = vinCode.ToUpper();
		_vinValidator.ValidateAndThrowFriendly(vinCode);

		using var session = _sessionFactory.OpenPostgresConnection().StartTrace().BeginTransaction();
		var dbCar = await _carRepository.SearchByVinCode(session, vinCode, ct);
		if (dbCar is not null)
		{
			if (dbCar.IsVinValid)
				return _carMapper.ToCarDto(dbCar);
			else
				throw new UserFriendlyException(VinValidationCodes.InvalidVinCode, "VIN код невалиден");
		}

		var decoderVinRequestUrl = BuildVinDecodeRequest(vinCode);
		var decoderVinResponse = await ExecuteCloudApiRequest<VinDecoderResponse>(decoderVinRequestUrl, ct);
		if (decoderVinResponse.FinalException is not null)
		{
			_logger.Error(decoderVinResponse.FinalException, "Failed to decode VIN. Request {Request}", decoderVinRequestUrl);
			throw decoderVinResponse.FinalException;
		}
		var decodedVin = decoderVinResponse.Result;
		if (decodedVin.Found == false)
		{
			dbCar = new Car
			{
				Id = Guid.NewGuid(),
				Vin = vinCode,
				Created = start,
				IsVinValid = false,
			};
			await _carRepository.Insert(session, dbCar, CancellationToken.None);
			throw new UserFriendlyException(VinValidationCodes.InvalidVinCode, "VIN код невалиден");
		}

		var convertVinRequestUrl = BuildVinConvertRequest(vinCode);
		var convertVinTask = ExecuteCloudApiRequest<VinConverterResponse>(convertVinRequestUrl, ct);

		var gibddVinRequestUrl = BuildGibddVinRequest(vinCode);
		var gibddVinTask = ExecuteCloudApiRequest<GibddServiceVinSearchResponse>(gibddVinRequestUrl, ct);

		var convertVinResponse = await convertVinTask;
		var gibddVin = await gibddVinTask;

		string? modelName = null;
		if (convertVinResponse.Result.Partner?.Result?.BrandModel?.IsNullOrWhiteSpace() == false)
		{
			if (gibddVin.Result?.Vehicle?.Color?.IsNullOrWhiteSpace() == false)
			{
				var color = gibddVin.Result.Vehicle.Color.ToLower();
				color = char.ToUpper(color[0]) + color.Substring(1);
				modelName = $"{color} {convertVinResponse.Result.Partner.Result.BrandModel}";
			}
			else
			{
				modelName = convertVinResponse.Result.Partner.Result.BrandModel;
			}
		}
		else if (decodedVin.Make?.Value?.IsNullOrWhiteSpace() == false && decodedVin.Model?.Value?.IsNullOrWhiteSpace() == false)
		{
			if (gibddVin.Result?.Vehicle?.Color?.IsNullOrWhiteSpace() == false)
			{
				var color = gibddVin.Result.Vehicle.Color.ToLower();
				color = char.ToUpper(color[0]) + color.Substring(1);
				modelName = $"{color} {decodedVin.Make.Value} {decodedVin.Model.Value}";
			}
			else
			{
				modelName = $"{decodedVin.Make.Value} {decodedVin.Model.Value}";

			}
		}
		else if (gibddVin.Result?.Vehicle?.Model?.IsNullOrWhiteSpace() == false)
		{
			string color = "";
			if (gibddVin.Result.Vehicle.Color?.IsNullOrWhiteSpace() == false)
			{
				color = gibddVin.Result.Vehicle.Color.ToLower();
				color = char.ToUpper(color[0]) + color.Substring(1) + ' ';
			}

			modelName = $"{color}{gibddVin.Result?.Vehicle?.Model}";
		}

		int.TryParse(decodedVin.NumberSeats?.Value ?? "0", out var seatsCount);

		dbCar = new Car
		{
			Id = Guid.NewGuid(),
			Created = start,
			DoesVinAndRegistrationNumberMatches = convertVinResponse.Result.Partner?.Found == true,
			IsDeleted = false,
			IsVinValid = convertVinResponse.Result.Partner?.Found == true,
			Name = modelName,
			RegistrationNumber = convertVinResponse.Result?.Partner?.Result?.RegNumber,
			SeatsCount = seatsCount,
			Vin = vinCode,
		};
		await _carRepository.Insert(session, dbCar, CancellationToken.None);

		var result = _carMapper.ToCarDto(dbCar);
		return result;
	}

	public async Task<DriverData> ValidateDriverLicense(Guid userId, Models.DriverServiceModels.Driver driver, CancellationToken ct)
	{
		var start = _clock.Now;
		_driverValidator.ValidateAndThrowFriendly(driver);

		using var session = _sessionFactory.OpenPostgresConnection().BeginTransaction().StartTrace();

		var actualPersonData = await _personDataRepository.GetByUserId(session, userId, ct);

		if (actualPersonData is null)
		{
			throw new UserFriendlyException(DriverValidatorCodes.EmptyPassport, "Водительское удостоверение можно заполнить только при наличии подтверждённого действительного паспорта.");
		}

		var driverData = await _driverDataRepository.GetByDrivingLicense(
			session: session,
			licenseSeries: driver.LicenseSeries,
			licenseNumber: driver.LicenseNumber,
			ct: ct);

		if (driverData is not null)
		{
			if (driverData.Issuance != driver.Issuance || driverData.BirthDate != actualPersonData.BirthDate)
			{
				throw new UserFriendlyException(DriverValidatorCodes.IncorrectDriverLicenseData, "Неверные данные водительского удостоверения");
			}

			if (driverData.UserId != actualPersonData.UserId)
			{
				throw new UserFriendlyException(DriverValidatorCodes.DrivingLicenseWasUsedForAnotherUser, "Водительское удостоверение уже используется другим пользователем. Если Вы считаете, что Ваше водительское удостоверение было неправомерно использовано сторонним лицом, сообщите об этом в техническую поддержку");
			}

			return driverData;
		}

		ct.ThrowIfCancellationRequested();

		var requestUrl = BuildGibddDriverLicenseRequest(driver);
		var getGibddResponse = await ExecuteCloudApiRequest<GibddServiceDrivingLicenseResponse>(requestUrl, ct);

		if (getGibddResponse.FinalException is not null)
		{
			_logger.Error(getGibddResponse.FinalException, "Failed to get driver license info. Request {Request}", requestUrl);
			throw getGibddResponse.FinalException;
		}

		var gibddResponse = getGibddResponse.Result;

		if (gibddResponse is null || gibddResponse.Status != 200)
		{
			var json = gibddResponse is null
				? "{}"
				: JsonConvert.SerializeObject(gibddResponse);

			_logger.Error("Failed to get driver license info. Request {Request}, Response {Response}",
				requestUrl, json);
			throw new Exception("Failed to get driver license info");
		}

		if (gibddResponse.Found == false || gibddResponse.Doc?.Num.IsNullOrWhiteSpace() != false)
			throw new UserFriendlyException(DriverValidatorCodes.IncorrectDriverLicenseData, "Неверные данные водительского удостоверения");

		driverData = _gibddMapper.MapToDriverData(gibddResponse);
		driverData.Id = Guid.NewGuid();
		driverData.Created = start;
		driverData.IsValid = true;
		driverData.LastCheckDate = start;

		if (driverData.BirthDate == actualPersonData.BirthDate)
			driverData.UserId = userId;
		else
			driverData.UserId = null;

		await _driverDataRepository.Insert(session, driverData, CancellationToken.None);
		await session.CommitAsync(CancellationToken.None);

		if (driverData.BirthDate != actualPersonData.BirthDate)
			throw new UserFriendlyException(DriverValidatorCodes.IncorrectDriverLicenseData, "Неверные данные водительского удостоверения");

		// TODO - проверка действительности водительского удостоверения.
		// Технически, этого можно достичь, проверив, что сейчас нет активных лишений, взглянув на поле
		// GibddServiceDrivingLicenseResponse.Decis и посмотрев все пересечения с текущей датой.
		// Но как-то запарно, а на этапе разработки не было Api ключа, чтобы всё потестить,
		// водительского удостоверения с лишениями тоже не было под рукой.
		// Пока на это забили.

		return driverData;
	}

	public async Task<PersonData> ValidatePerson(IPostgresSession session, Person person, CancellationToken ct)
	{
		var start = _clock.Now;
		person.Normalize();
		_personValidator.ValidateAndThrowFriendly(person);

		ct.ThrowIfCancellationRequested();

		// Если паспортные данные неверны, не удастся получить ИНН.
		(var inn, var personData) = await GetInn(session, person, ct);

		if (personData is null)
		{
			personData = _personMapper.MapToPersonData(person);
			personData.Inn = inn;
			personData.WasCheckedAtLeastOnce = false;
			personData.Created = start;
			personData.IsPassportValid = true;
			personData.Id = Guid.NewGuid();

			await _personDataRepository.Insert(session, personData, CancellationToken.None);
		}

		ct.ThrowIfCancellationRequested();

		if (NeedToCheckPassport(personData))
		{
			await CheckPassport(personData);
		}

		return personData;
	}

	private bool NeedToCheckPassport(PersonData personData)
	{
		if (_config.NeedToCheckPassport == false)
			return false;

		if (personData.WasCheckedAtLeastOnce == false)
			return true;

		if (_clock.Now.Add(-_config.TrustPassportPeriod) > personData.LastCheckPassportDate && personData.IsPassportValid == true)
			return true;

		return false;
	}

	private ValueTask CheckPassport(PersonData personData)
	{
		// На момент написания кода сервис проверки паспортов работал только по архивным данным из-за проблем МВД. В таких условиях проверкой паспорта решено пренебречь, так как проверка на существование паспорта неявно осуществляется получением ИНН.
		throw new NotImplementedException();
	}

	/// <summary>
	/// Получить ИНН.
	/// </summary>
	/// <returns>
	/// В случае ошибок получения ИНН выкинет ошибку. Если ИНН был взят из БД, то вместе с ИНН вернёт <see cref="PersonData"/>. В противном случае только ИНН.
	/// </returns>
	private async ValueTask<(long Inn, PersonData? PersonData)> GetInn(IPostgresSession session, Person person, CancellationToken ct)
	{
		person.Normalize();

		var personData = await _personDataRepository.GetByPassport(
			session: session,
			passportSeries: person.PassportSeries,
			passportNumber: person.PassportNumber,
			ct: ct);

		if (personData is not null)
		{
			if (AreEqual(person, personData) && personData.IsPassportValid == true)
				return (personData.Inn, personData);
			else
				throw new UserFriendlyException(PersonValidatorCodes.IncorrectPersonData, "Некорректные данные пользователя");
		}

		var requestUrl = BuildInnRequest(person);

		var getInnResponse = await ExecuteCloudApiRequest<TaxServiceInnResponse>(requestUrl, ct);

		if (getInnResponse.FinalException is not null)
		{
			_logger.Error(getInnResponse.FinalException, "Failed to get inn. Request {Request}", requestUrl);
			throw getInnResponse.FinalException;
		}

		var innResponse = getInnResponse.Result;

		if (innResponse is null || innResponse.Status != 200)
		{
			var json = innResponse is null
				? "{}"
				: JsonConvert.SerializeObject(innResponse);

			_logger.Error("Failed to get inn. Request {Request}, Response {Response}",
				requestUrl, json);
			throw new Exception("Failed to get inn");
		}

		if (innResponse.Found == false || innResponse.Inn.IsNullOrWhiteSpace())
			throw new UserFriendlyException(PersonValidatorCodes.IncorrectPersonData, "Некорректные данные пользователя");

		return (long.Parse(innResponse.Inn), null);
	}

	private bool AreEqual(Person person, PersonData personData)
	{
		return person.PassportSeries == personData.PassportSeries
			&& person.PassportNumber == personData.PassportNumber
			&& person.BirthDate == personData.BirthDate
			&& string.Equals(person.FirstName, personData.FirstName, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(person.LastName, personData.LastName, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(person.SecondName, personData.SecondName, StringComparison.OrdinalIgnoreCase);
	}

	private string BuildInnRequest(Person person)
	{
		var sb = new StringBuilder(_taxServiceBaseUrl);
		sb.Append("?type=inn&firstname=");
		sb.Append(person.FirstName);
		sb.Append("&lastname=");
		sb.Append(person.LastName);

		if (person.SecondName.IsNullOrEmpty() == false)
		{
			sb.Append("&secondname=");
			sb.Append(person.SecondName);
		}

		sb.Append("&birthdate=");
		sb.AppendFormat("{0:dd.MM.yyyy}", person.BirthDate);

		sb.Append("&serianomer=");
		sb.AppendFormat("{0:0000}", person.PassportSeries);
		sb.AppendFormat("{0:000000}", person.PassportNumber);

		sb.Append("&token=");
		sb.Append(_config.ApiCloudApiKey);

		return sb.ToString();
	}

	private string BuildGibddDriverLicenseRequest(Models.DriverServiceModels.Driver driver)
	{
		var sb = new StringBuilder(_gibddServiceBaseUrl);
		sb.Append("?type=driver&serianomer=");
		sb.AppendFormat("{0:0000}", driver.LicenseSeries);
		sb.AppendFormat("{0:000000}", driver.LicenseNumber);

		sb.Append("&date=");
		sb.AppendFormat("{0:dd.MM.yyyy}", driver.Issuance);

		sb.Append("&token=");
		sb.Append(_config.ApiCloudApiKey);

		return sb.ToString();
	}

	private string BuildVinConvertRequest(string vin)
	{
		var result = $"{_vinConverterServiceBaseUrl}?type=search&string={vin}&token={_config.ApiCloudApiKey}";
		return result;
	}

	private string BuildVinDecodeRequest(string vin)
	{
		var result = $"{_vinDecoderServiceBaseUrl}?type=vin&vin={vin}&token={_config.ApiCloudApiKey}";
		return result;
	}

	private string BuildGibddVinRequest(string vin)
	{
		var result = $"{_gibddServiceBaseUrl}?type=gibdd&vin={vin}&token={_config.ApiCloudApiKey}";
		return result;
	}

	private async Task<PolicyResult<T>> ExecuteCloudApiRequest<T>(string requestUrl, CancellationToken ct)
	{
		var cloudApiResponse = await _asyncPolicy.ExecuteAndCaptureAsync(async internalCt =>
		{
			using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			using var response = await _httpClient.SendAsync(httpRequest, internalCt);
			response.EnsureSuccessStatusCode();
			var body = await response.Content.ReadAsStringAsync(CancellationToken.None);

			SaveCloudApiResponse(requestUrl, body);

			var innResponse = JsonConvert.DeserializeObject<T>(body);
			return innResponse;
		}, ct);

		return cloudApiResponse!;
	}

	private void SaveCloudApiResponse(string request, string response)
	{
		var info = new CloudApiResponseInfo
		{
			Id = Guid.NewGuid(),
			Created = _clock.Now,
			Request = request,
			RequestBasePath = request.Substring(0, request.IndexOf("?")),
			Response = response.IsNullOrWhiteSpace() ? "{}" : response,
		};

		_cloudApiResponsesForSave.Enqueue(info);
	}

	private async Task StartSaverCloudApiResponseBackgroundTask()
	{
		while (true)
		{
			if (_cloudApiResponsesForSave.Count < _config.CloudApiResponseSaverMaxBatchCount)
				await Task.Delay(_config.CloudApiResponseSaverPollingDelay);

			try
			{
				var count = _cloudApiResponsesForSave.Count;
				if (count == 0)
					continue;

				if (count > _config.CloudApiResponseSaverMaxBatchCount)
					count = _config.CloudApiResponseSaverMaxBatchCount;

				using var session = _sessionFactory.OpenPostgresConnection().BeginTransaction().StartTrace();

				var infos = new List<CloudApiResponseInfo>(count);

				for (int i = 0; i < count; i++)
				{
					if (_cloudApiResponsesForSave.TryDequeue(out var info))
					{
						infos.Add(info);
					}
				}

				await _cloudApiResponseInfoRepository.BulkInsert(session, infos, CancellationToken.None);
				await session.CommitAsync(CancellationToken.None);
				_logger.Information("Saved {Count} CloudApi responses", infos.Count);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to save CloudApi responses.");
			}
		}
	}
}