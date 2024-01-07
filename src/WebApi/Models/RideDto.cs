using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class RideDto
	{
		public Guid Id { get; set; }
		public ulong DriverId { get; set; }
	}

	public interface IRideDtoMapper : IBaseMapper<Ride, RideDto>
	{
	}

	[Mapper]
	public partial class RideDtoMapper : BaseMapper<Ride, RideDto>, IRideDtoMapper
	{
		public RideDtoMapper() : base(() => new(), () => new())
		{
		}

		private partial void ToDtoAuto(Ride ride, RideDto dto);

		private partial void FromDtoAuto(RideDto dto, Ride ride);

		private partial void BetweenDtosAuto(RideDto from, RideDto to);
		private partial void BetweenEntitiesAuto(Ride from, Ride to);


		protected override void BetweenDtos(RideDto from, RideDto to)
			=> BetweenDtosAuto(from, to);
		protected override void BetweenEntities(Ride from, Ride to)
			=> BetweenEntitiesAuto(from, to);
		protected override void FromDtoAbstract(RideDto dto, Ride entity, IDictionary<object, object> mappedObjects)
			=> FromDtoAuto(dto, entity);
		protected override void ToDtoAbstract(Ride entity, RideDto dto, IDictionary<object, object> mappedObjects)
			=> ToDtoAuto(entity, dto);
	}
}
