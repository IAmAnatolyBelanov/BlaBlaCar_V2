namespace WebApi;

public static class MathExtensions
{
	public static int RoundByStep(double value, int step, bool onlyGreaterThanZero = true)
	{
		var result = (int)Math.Round(value / step) * step;
		if (onlyGreaterThanZero && result <= 0)
			result = step;

		return result;
	}

	public static int RoundByStep(int value, int step, bool onlyGreaterThanZero = true)
	{
		var result = (int)Math.Round((decimal)value / step) * step;
		if (onlyGreaterThanZero && result <= 0)
			result = step;

		return result;
	}
}
