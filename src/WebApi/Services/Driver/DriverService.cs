using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Polly;
using System.Text;
using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Models.DriverServiceModels;
using WebApi.Services.Validators;

namespace WebApi.Services.Driver;

public interface IDriverService
{
	Task<DriverData> ValidateDriverLicense(User user, Models.DriverServiceModels.Driver driver, CancellationToken ct);
	Task<PersonData> ValidatePerson(Person person, CancellationToken ct);
}

public class DriverService : IDriverService
{
	private readonly IServiceScopeFactory _serviceScopeFactory;
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
	private readonly IAsyncPolicy _asyncPolicy;

	public DriverService(
		IServiceScopeFactory serviceScopeFactory,
		IDriverServiceConfig driverServiceConfig,
		IValidator<Person> personValidator,
		IPersonMapper personMapper,
		IClock clock,
		IValidator<Models.DriverServiceModels.Driver> driverValidator,
		IGibddServiceDrivingLicenseResponseMapper gibddMapper)
	{
		_serviceScopeFactory = serviceScopeFactory;
		_config = driverServiceConfig;
		_personValidator = personValidator;
		_personMapper = personMapper;
		_clock = clock;
		_driverValidator = driverValidator;
		_gibddMapper = gibddMapper;

		var baseUrl = new Uri(_config.ApiCloudBaseUrl);

		_taxServiceBaseUrl = new Uri(baseUrl, "nalog.php").AbsoluteUri;
		_mvdServiceBaseUrl = new Uri(baseUrl, "mvd.php").AbsoluteUri;
		_gibddServiceBaseUrl = new Uri(baseUrl, "gibdd.php").AbsoluteUri;

		_asyncPolicy = Policy
			.Handle<HttpRequestException>()
			.WaitAndRetryAsync(_config.RetryCount,
			attempt => _config.RetryDelay,
			(exception, timeSpan, attempt, context) =>
			{
				_logger.Error("Failed to fetch api-cloud response. Attempt {Attempt}, time delay {TimeDelay}, context {Context}, exception {Exception}",
					attempt, timeSpan, context, exception);
			});
	}

	public async Task<DriverData> ValidateDriverLicense(User user, Models.DriverServiceModels.Driver driver, CancellationToken ct)
	{
		var start = _clock.Now;
		_driverValidator.ValidateAndThrowFriendly(driver);

		using var scope = BuildScope();
		using var context = GetDbContext(scope);

		var actualPersonData = user.ActualPersonData
			?? await context.PersonDatas
				.Where(x => x.IsPassportValid && x.UserId == user.Id)
				.OrderByDescending(x => x.Created)
				.FirstOrDefaultAsync(ct);

		if (actualPersonData is null)
		{
			throw new UserFriendlyException(DriverValidatorCodes.EmptyPassport, "Водительское удостоверение можно заполнить только при наличии подтверждённого действительного паспорта.");
		}

		var driverData = await context.DriverDatas
			.FirstOrDefaultAsync(x => x.LicenseSeries == driver.LicenseSeries && x.LicenseNumber == driver.LicenseNumber, ct);

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
			driverData.UserId = user.Id;
		else
			driverData.UserId = null;

		await context.DriverDatas.AddAsync(driverData, CancellationToken.None);
		await context.SaveChangesAsync(CancellationToken.None);

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

	public async Task<PersonData> ValidatePerson(Person person, CancellationToken ct)
	{
		var start = _clock.Now;
		person.Normalize();
		_personValidator.ValidateAndThrowFriendly(person);

		using var scope = BuildScope();
		using var context = GetDbContext(scope);

		// Если паспортные данные неверны, не удастся получить ИНН.
		(var inn, var personData) = await GetInn(context, person, ct);

		if (personData is null)
		{
			personData = _personMapper.MapToPersonData(person);
			personData.Inn = inn;
			personData.WasCheckedAtLeastOnce = false;
			personData.Created = start;
			personData.IsPassportValid = true;
			personData.Id = Guid.NewGuid();

			await context.PersonDatas.AddAsync(personData, CancellationToken.None);
			await context.SaveChangesAsync(CancellationToken.None);
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
	private async ValueTask<(long Inn, PersonData? PersonData)> GetInn(ApplicationContext context, Person person, CancellationToken ct)
	{
		person.Normalize();

		var personData = await context.PersonDatas.FirstOrDefaultAsync(x => x.PassportSeries == person.PassportSeries && x.PassportNumber == person.PassportNumber, ct);

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

	private async ValueTask<PolicyResult<T>> ExecuteCloudApiRequest<T>(string requestUrl, CancellationToken ct)
	{
		var cloudApiResponse = await _asyncPolicy.ExecuteAndCaptureAsync(async internalCt =>
		{
			using var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
			using var response = await _httpClient.SendAsync(httpRequest, internalCt);
			response.EnsureSuccessStatusCode();
			var body = await response.Content.ReadAsStringAsync(internalCt);

			_ = SaveCloudApiResponse(requestUrl, body);

			var innResponse = JsonConvert.DeserializeObject<T>(body);
			return innResponse;
		}, ct);

		return cloudApiResponse!;
	}

	private async Task SaveCloudApiResponse(string request, string response)
	{
		await Task.CompletedTask;

		var info = new CloudApiResponseInfo
		{
			Id = Guid.NewGuid(),
			Created = _clock.Now,
			Request = request,
			RequestBasePath = request.Substring(0, request.IndexOf("?")),
			Response = response.IsNullOrWhiteSpace() ? "{}" : response,
		};
		using var scope = BuildScope();
		using var context = GetDbContext(scope);

		await context.CloudApiResponseInfos.AddAsync(info);
		await context.SaveChangesAsync();
	}

	private AsyncServiceScope BuildScope()
		=> _serviceScopeFactory.CreateAsyncScope();
	private ApplicationContext GetDbContext(AsyncServiceScope scope)
		=> scope.ServiceProvider.GetRequiredService<ApplicationContext>();
}