using WebApi;

namespace Tests;

public class MathExtensionsTests
{
	[Theory]
	[InlineData(10, 5, 10)]
	[InlineData(11, 5, 10)]
	[InlineData(14, 5, 15)]
	[InlineData(15, 5, 15)]
	[InlineData(17.5, 5, 20)]
	[InlineData(7.5, 5, 10)]
	[InlineData(12.5, 5, 10)]
	[InlineData(22.5, 5, 20)]
	[InlineData(1, 5, 5)]
	public void TestRoundDoubleByStep(double value, int step, int expected)
	{
		var result = MathExtensions.RoundByStep(value, step);
		result.Should().Be(expected);
	}

	[Theory]
	[InlineData(10, 5, 10)]
	[InlineData(9, 5, 10)]
	[InlineData(11, 5, 10)]
	[InlineData(1, 5, 5)]
	public void TestRoundIntByStep(int value, int step, int expected)
	{
		var result = MathExtensions.RoundByStep(value, step);
		result.Should().Be(expected);
	}
}
