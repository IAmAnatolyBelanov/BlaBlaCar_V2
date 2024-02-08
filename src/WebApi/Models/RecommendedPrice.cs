using System.Diagnostics;

namespace WebApi;

[DebuggerDisplay("{ToString(),nq}")]
public struct RecommendedPrice
{
	public int Low { get; set; }
	public int Average { get; set; }
	public int High { get; set; }
	public int Step { get; set; }

	public override string ToString()
		=> @$"{{""{nameof(Low)}"":{Low},""{nameof(Average)}"":{Average},""{nameof(High)}"":{High},""{nameof(Step)}"":{Step}}}";
}
