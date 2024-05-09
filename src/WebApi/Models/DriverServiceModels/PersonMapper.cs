using Riok.Mapperly.Abstractions;

namespace WebApi.Models.DriverServiceModels;

public interface IPersonMapper
{
	PersonData MapToPersonData(Person person);
}

[Mapper]
public partial class PersonMapper : IPersonMapper
{
	public partial PersonData MapToPersonData(Person person);
}
