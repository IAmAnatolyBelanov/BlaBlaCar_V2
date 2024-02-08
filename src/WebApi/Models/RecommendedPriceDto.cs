using Riok.Mapperly.Abstractions;

namespace WebApi;

public struct RecommendedPriceDto
{
	public int Low { get; set; }
	public int Average { get; set; }
	public int High { get; set; }
	public int Step { get; set; }
}

public interface IRecommendedPriceDtoMapper
{
	RecommendedPrice FromDto(RecommendedPriceDto dto);
	RecommendedPriceDto ToDto(RecommendedPrice price);
}

[Mapper]
public partial class RecommendedPriceDtoMapper : IRecommendedPriceDtoMapper
{
	public partial RecommendedPrice FromDto(RecommendedPriceDto dto);

	public partial RecommendedPriceDto ToDto(RecommendedPrice price);
}