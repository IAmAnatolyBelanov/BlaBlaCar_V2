
namespace WebApi.Services.Validators;

public class VinValidationCodes : ValidationCodes
{
	public const string InvalidLength = "VanValidation_InvalidLength";
	public const string ContainsInvalidSymbols = "VanValidation_ContainsInvalidSymbols";
	public const string InvalidVinCode = "VanValidation_InvalidVinCode";
}

public interface IVinValidator
{
	void ValidateAndThrowFriendly(string vin);
}

public class VinValidator : IVinValidator
{
	private static readonly IReadOnlyDictionary<char, int> _characterWeights = new Dictionary<char, int>
	{
		['0'] = 0,
		['1'] = 1,
		['2'] = 2,
		['3'] = 3,
		['4'] = 4,
		['5'] = 5,
		['6'] = 6,
		['7'] = 7,
		['8'] = 8,
		['9'] = 9,

		['A'] = 1,
		['B'] = 2,
		['C'] = 3,
		['D'] = 4,
		['E'] = 5,
		['F'] = 6,
		['G'] = 7,
		['H'] = 8,

		['J'] = 1,
		['K'] = 2,
		['L'] = 3,
		['M'] = 4,
		['N'] = 5,
		// Skip O
		['P'] = 7,
		// Skip I
		['R'] = 9,

		// Skip Q
		['S'] = 2,
		['T'] = 3,
		['U'] = 4,
		['V'] = 5,
		['W'] = 6,
		['X'] = 7,
		['Y'] = 8,
		['Z'] = 9,
	};

	private IReadOnlyDictionary<char, int> _reminderEquality = new Dictionary<char, int>
	{
		['0'] = 0,
		['1'] = 1,
		['2'] = 2,
		['3'] = 3,
		['4'] = 4,
		['5'] = 5,
		['6'] = 6,
		['7'] = 7,
		['8'] = 8,
		['9'] = 9,
		['X'] = 10,
	};

	private static readonly IReadOnlyList<int> _indexWeights = new int[] { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

	public void ValidateAndThrowFriendly(string vin)
	{
		if (vin.Length != 17)
			throw new UserFriendlyException(VinValidationCodes.InvalidLength, "Длина VIN кода должна быть 17 символов");

		var sum = 0;
		for (int i = 0; i < vin.Length; i++)
		{
			var symbol = vin[i];
			if (!_characterWeights.TryGetValue(symbol, out var weight))
				throw new UserFriendlyException(VinValidationCodes.ContainsInvalidSymbols, "VIN код содержит невалидные символы");
			sum += weight * _indexWeights[i];
		}

		var reminder = sum % 11;
		if (_reminderEquality[vin[8]] != reminder)
			throw new UserFriendlyException(VinValidationCodes.InvalidVinCode, "VIN код невалиден");
	}
}