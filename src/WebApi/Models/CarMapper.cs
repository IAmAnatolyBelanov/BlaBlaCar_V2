using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface ICarMapper
{
	CarDto ToCarDto(Car car);
}

[Mapper]
public partial class CarMapper : ICarMapper
{
	public partial CarDto ToCarDto(Car car);
}