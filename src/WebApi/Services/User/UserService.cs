using FluentValidation;
using WebApi.DataAccess;
using WebApi.Models.DriverServiceModels;
using WebApi.Repositories;
using WebApi.Services.Driver;

namespace WebApi.Services.User;

public interface IUserService
{
	Task RegisterUser(Models.User user, CancellationToken ct);
	Task UpdatePersonData(Guid userId, Person person, CancellationToken ct);
}

public class UserService : IUserService
{
	private readonly ILogger _logger = Log.ForContext<UserService>();

	private readonly IUserRepository _userRepository;
	private readonly ISessionFactory _sessionFactory;
	private readonly IDriverService _driverService;
	private readonly IPersonDataRepository _personDataRepository;
	private readonly IDriverDataRepository _driverDataRepository;

	public UserService(
		IUserRepository userRepository,
		ISessionFactory sessionFactory,
		IDriverService driverService,
		IPersonDataRepository personDataRepository,
		IDriverDataRepository driverDataRepository)
	{
		_userRepository = userRepository;
		_sessionFactory = sessionFactory;
		_driverService = driverService;
		_personDataRepository = personDataRepository;
		_driverDataRepository = driverDataRepository;
	}

	public async Task RegisterUser(Models.User user, CancellationToken ct)
	{
		using var session = _sessionFactory.OpenPostgresConnection().BeginTransaction();
		await _userRepository.Insert(session, user, ct);
		await session.CommitAsync(ct);
	}

	public async Task UpdatePersonData(Guid userId, Person person, CancellationToken ct)
	{
		// Намеренно не начинаем транзакцию, чтобы каждое действие в бд было сохранено.
		// Например, добавление паспортных данных, даже если отмена была запрошена.
		using var session = _sessionFactory.OpenPostgresConnection().StartTrace();

		var currentPersonData = await _personDataRepository.GetByUserId(session, userId, ct);

		var checkedPersonData = await _driverService.ValidatePerson(session, person, ct);

		if (checkedPersonData.Id == currentPersonData?.Id)
		{
			_logger.Information("For user {UserId} did not updated person data - person data is the same as existing one", userId);
			return;
		}

		if (checkedPersonData.UserId.HasValue && checkedPersonData.UserId.Value != userId)
		{
			throw new UserFriendlyException("ПридуматьКод", "Паспорт уже использован кем-то ещё. Обратитесь к поддержке.");
		}

		if (currentPersonData is null || checkedPersonData.UserId == null)
		{
			checkedPersonData.UserId = userId;
			await _personDataRepository.UpdateUserId(session, checkedPersonData.Id, userId, ct);
			_logger.Information("For user {UserId} set person data {PersonDataId}", userId, checkedPersonData.Id);
			return;
		}

		if (currentPersonData is null || checkedPersonData.Id != currentPersonData.Id)
		{
			session.BeginTransaction();
			if (currentPersonData is not null)
			{
				await _personDataRepository.DisablePersonData(session, currentPersonData.Id, ct);
				_logger.Information("For user {UserId} disabled person data {PersonDataId}", userId, currentPersonData.Id);
			}

			checkedPersonData.UserId = userId;
			await _personDataRepository.UpdateUserId(session, checkedPersonData.Id, userId, ct);
			_logger.Information("For user {UserId} set person data {PersonDataId}", userId, checkedPersonData.Id);

			await session.CommitAsync(ct);
			return;
		}

		throw new Exception("Broken logic. Person data is new, but the same.");
	}
}
