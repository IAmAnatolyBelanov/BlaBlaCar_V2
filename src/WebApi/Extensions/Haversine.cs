using WebApi.Models;

namespace WebApi.Extensions;

// https://rosettacode.org/wiki/Haversine_formula#C#
// https://learn.microsoft.com/en-us/answers/questions/1394844/how-can-i-get-the-distance-in-miles-between-twonet
public static class Haversine
{
	public static double CalculateDistanceInKilometers(FormattedPoint point1, FormattedPoint point2)
		=> CalculateDistanceInKilometers(point1.Latitude, point1.Longitude, point2.Latitude, point2.Longitude);

	public static double CalculateDistanceInKilometers(double lat1, double lon1, double lat2, double lon2)
	{
		const double R = 6372.8; // In kilometers
		var dLat = ToRadians(lat2 - lat1);
		var dLon = ToRadians(lon2 - lon1);
		lat1 = ToRadians(lat1);
		lat2 = ToRadians(lat2);

		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
			+ Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);

		var c = 2 * Math.Asin(Math.Sqrt(a));
		return R * c;
	}

	private static double ToRadians(double val)
	{
		return Math.PI * val / 180.0;
	}
}
