using FluentValidation;
using WebApi.Models.DriverServiceModels;

namespace WebApi.Services.Validators;

public class PersonValidatorCodes : ValidationCodes
{
	public const string IncorrectPersonData = "PersonValidator_IncorrectPersonData";

	public const string IncorrectPassportSeries = "PersonValidator_IncorrectPassportSeries";
	public const string IncorrectPassportNumber = "PersonValidator_IncorrectPassportNumber";
	public const string IncorrectDateOfBirth = "PersonValidator_IncorrectDateOfBirth";
	public const string IncorrectFirstName = "PersonValidator_IncorrectFirstName";
	public const string IncorrectLastName = "PersonValidator_IncorrectLastName";
	public const string IncorrectSecondName = "PersonValidator_IncorrectSecondName";
	public const string TooYoung = "PersonValidator_AgeMustBeGreaterThanOrEqualsToMinAges";
	public const string IncorrectBirthDate = "PersonValidator_IncorrectBirthDate";
}

public class PersonValidator : AbstractValidator<Person>
{
	// TODO - проверить апостроф (это который ’ по документу, но ' по инфы из гугла).

	// В соответствии с https://www.nalog.gov.ru/html/sites/www.rn74.nalog.ru/doc74/novaya_forma.pdf .
	// Пробел добавлен не случайно - он тоже допустим или недопустим.
	private static readonly IReadOnlySet<char> _validNameSymbols = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ -.,'() IV"
		.ToHashSet();

	private static readonly IReadOnlySet<char> _bannedFirstOrLastOrSingleSymbolsForFirstAndSecondNames
		= "-' ,".ToHashSet();

	private static readonly IReadOnlySet<char> _bannedFirstOrLastOrSingleSymbolsForLastNames
		= ".-' ,".ToHashSet();

	private static readonly IReadOnlySet<char> _bannedDoubleSymbols = ".-' ,()".ToHashSet();

	/// <summary>
	/// Не допускается наличия подряд идущих символов:
	/// </summary>
	private static readonly IReadOnlySet<char> _bannedSymbolsInSuccession = ".-',()".ToHashSet();

	private readonly IClock _clock;
	private readonly IDriverServiceConfig _config;

	public PersonValidator(IClock clock, IDriverServiceConfig config)
	{
		_clock = clock;
		_config = config;

		RuleFor(x => x.PassportSeries)
			.GreaterThanOrEqualTo(1)
			.WithErrorCode(PersonValidatorCodes.IncorrectPassportSeries)
			.WithMessage("Серия паспорта должна находиться в диапазоне от 0001 до 9999");

		RuleFor(x => x.PassportSeries)
			.LessThanOrEqualTo(9_999)
			.WithErrorCode(PersonValidatorCodes.IncorrectPassportSeries)
			.WithMessage("Серия паспорта должна находиться в диапазоне от 0001 до 9999");

		RuleFor(x => x.PassportNumber)
			.GreaterThanOrEqualTo(1)
			.WithErrorCode(PersonValidatorCodes.IncorrectPassportNumber)
			.WithMessage("Номер паспорта должен находиться в диапазоне от 000001 до 999999");

		RuleFor(x => x.PassportNumber)
			.LessThanOrEqualTo(999_999)
			.WithErrorCode(PersonValidatorCodes.IncorrectPassportNumber)
			.WithMessage("Номер паспорта должен находиться в диапазоне от 000001 до 999999");

		RuleFor(x => x.FirstName)
			.NotEmpty()
			.WithErrorCode(PersonValidatorCodes.IncorrectFirstName)
			.WithMessage("Имя не может быть пустым")
			.DependentRules(() =>
			{
				RuleFor(x => x.FirstName)
					.Must(x => IsFirstOrSecondNameValid(x))
					.WithErrorCode(PersonValidatorCodes.IncorrectFirstName)
					.WithMessage("Некорректное имя");
			});

		RuleFor(x => x.LastName)
			.NotEmpty()
			.WithErrorCode(PersonValidatorCodes.IncorrectLastName)
			.WithMessage("Фамилия не может быть пустой")
			.DependentRules(() =>
			{
				RuleFor(x => x.LastName)
					.Must(x => IsLastNameValid(x))
					.WithErrorCode(PersonValidatorCodes.IncorrectLastName)
					.WithMessage("Некорректная фамилия");
			});

		RuleFor(x => x.SecondName)
			.Must(x => x.IsNullOrEmpty() || IsFirstOrSecondNameValid(x))
			.WithErrorCode(PersonValidatorCodes.IncorrectSecondName)
			.WithMessage("Некорректное отчество");

		RuleFor(x => x.BirthDate)
			.NotEmpty()
			.WithErrorCode(PersonValidatorCodes.IncorrectBirthDate)
			.WithMessage("Не заполнена дата рождения");

		RuleFor(x => x.BirthDate)
			.LessThanOrEqualTo(_ => _clock.Now.AddYears(-_config.MinAgesForPassport))
			.WithErrorCode(PersonValidatorCodes.TooYoung)
			.WithMessage("Возраст ниже допустимого");
	}

	/// <summary>
	/// Является ли имя или отчество корректным.
	/// </summary>
	/// <remarks>
	/// Этот код хочется оптимизировать, но в текущем состоянии он точнее соответствует https://www.nalog.gov.ru/html/sites/www.rn74.nalog.ru/doc74/novaya_forma.pdf документу. Так как проверки всё равно не напряжные, то лучше не трогать.
	/// </remarks>
	public bool IsFirstOrSecondNameValid(ReadOnlySpan<char> name)
	{
		if (IsNameValid(name) == false)
			return false;

		var first = name[0];
		if (_bannedFirstOrLastOrSingleSymbolsForFirstAndSecondNames.Contains(first))
			return false;

		var last = name[name.Length - 1];
		if (_bannedFirstOrLastOrSingleSymbolsForFirstAndSecondNames.Contains(last))
			return false;

		if (first == '.')
			return false;

		if (first == ')')
			return false;

		if (last == '(')
			return false;

		return true;
	}

	/// <summary>
	/// Является ли фамилия корректной.
	/// </summary>
	/// <remarks>
	/// Этот код хочется оптимизировать, но в текущем состоянии он точнее соответствует https://www.nalog.gov.ru/html/sites/www.rn74.nalog.ru/doc74/novaya_forma.pdf документу. Так как проверки всё равно не напряжные, то лучше не трогать.
	/// </remarks>
	private bool IsLastNameValid(ReadOnlySpan<char> name)
	{
		if (IsNameValid(name) == false)
			return false;

		var first = name[0];
		if (_bannedFirstOrLastOrSingleSymbolsForLastNames.Contains(first))
			return false;

		var last = name[name.Length - 1];
		if (_bannedFirstOrLastOrSingleSymbolsForLastNames.Contains(last))
			return false;

		if (first == ')')
			return false;

		if (last == '(')
			return false;

		return true;
	}

	/// <summary>
	/// Общая проверка для имени, фамилии и отчества.
	/// </summary>
	private bool IsNameValid(ReadOnlySpan<char> name)
	{
		var length = name.Length;
		var first = name[0];

		for (int i = 0; i < length; i++)
		{
			if (!_validNameSymbols.Contains(name[i]))
				return false;
		}

		for (int i = 1; i < length; i++)
		{
			var current = name[i];
			var previous = name[i - 1];

			if (current == previous && _bannedDoubleSymbols.Contains(current))
				return false;
		}

		for (int i = 1; i < length; i++)
		{
			if (_bannedSymbolsInSuccession.Contains(name[i]) && _bannedSymbolsInSuccession.Contains(name[i - 1]))
				return false;
		}

		int openBracketCount = 0;
		int closeBracketCount = 0;
		for (int i = 0; i < length; i++)
		{
			if (name[i] == '(')
				openBracketCount++;
			if (name[i] == ')')
				closeBracketCount++;
		}

		// Вообще, эта проверка не гарантирует отсутствия непарных скобок.
		// Но что-то сложнее писать запарно, а сервис получения ИНН всё равно подстрахует.
		if (openBracketCount != closeBracketCount)
			return false;

		if (first == 'I' || first == 'V')
			return false;

		return true;
	}
}
