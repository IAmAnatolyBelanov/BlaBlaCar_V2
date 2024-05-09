namespace WebApi.Models;

public class User
{
	public Guid Id { get; set; }

	public List<PersonData>? PersonDatas { get; set; }

	public PersonData? ActualPersonData { get; set; }
}
