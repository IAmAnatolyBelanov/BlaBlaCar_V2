using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class RideDto
	{
		public Guid Id { get; set; }
		public ulong DriverId { get; set; }
	}

	public interface IRideDtoMapper
	{
		Ride FromDto(RideDto from);
		void FromDto(RideDto from, Ride to);
		RideDto ToDto(Ride from);
		void ToDto(Ride from, RideDto to);
	}

	[Mapper]
	public partial class RideDtoMapper : IRideDtoMapper
	{
		public partial void FromDto(RideDto from, Ride to);
		public Ride FromDto(RideDto from)
		{
			var result = new Ride();
			FromDto(from, result);
			return result;
		}

		public partial void ToDto(Ride from, RideDto to);
		public RideDto ToDto(Ride from)
		{
			var result = new RideDto();
			ToDto(from, result);
			return result;
		}
	}
}
