namespace WebApi.Models;

/// <summary>
/// Действия, что необходимо автоматически предпринять по окончанию <see cref="Ride.ValidationTimeBeforeDeparture"/>.
/// </summary>
public enum AfterRideValidationAction
{
	Unknown = 0,
	RejectionAndHideForNewPassengers,
	AutoAcceptWithoutValidation,
}