using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface IRideMapper
{
	Ride ToRide(RideDto dto);
	RideDto ToRideDto(Ride ride);
}

[Mapper]
public partial class RideMapper : IRideMapper
{
	public Ride ToRide(RideDto dto)
	{
		var result = new Ride();
		ToRide(dto, result);

		for (int i = 0; i < dto.PaymentMethods.Count; i++)
		{
			var method = dto.PaymentMethods[i];
			switch (method)
			{
				case PaymentMethod.Cash:
				result.IsCashPaymentMethodAvailable = true;
				break;
				case PaymentMethod.Cashless:
				result.IsCashlessPaymentMethodAvailable = true;
				break;
			}
		}
		return result;
	}

	public RideDto ToRideDto(Ride ride)
	{
		var dto = new RideDto();
		ToRideDto(ride, dto);

		var paymentMethods = new List<PaymentMethod>();
		if (ride.IsCashPaymentMethodAvailable)
			paymentMethods.Add(PaymentMethod.Cash);
		if (ride.IsCashlessPaymentMethodAvailable)
			paymentMethods.Add(PaymentMethod.Cashless);
		dto.PaymentMethods = paymentMethods;

		return dto;
	}

	private partial void ToRide(RideDto src, Ride target);
	private partial void ToRideDto(Ride src, RideDto target);
}
