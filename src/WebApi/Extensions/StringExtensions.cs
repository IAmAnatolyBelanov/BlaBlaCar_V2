using System.Diagnostics.CodeAnalysis;

namespace WebApi;

public static class StringExtensions
{
	public static bool IsNullOrWhiteSpace([NotNullWhen(false)] this string? value)
		=> string.IsNullOrWhiteSpace(value);

	public static bool IsNullOrEmpty([NotNullWhen(false)] this string? value)
		=> string.IsNullOrEmpty(value);

	public static string ReplaceApostrophes(this string value)
		=> value.Replace('’', '\'')
			.Replace('`', '\'')
			.Replace('´', '\'');

	public static string StartWithUpperCase(this string value)
	{
		if (value.Length <= 0)
			return value;

		if (char.IsUpper(value[0]))
			return value;

		if (value.Length == 1)
			return char.ToUpper(value[0]).ToString();

		return char.ToUpper(value[0]) + value.Substring(1);
	}
}
