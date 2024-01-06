using NetTopologySuite.Geometries;

namespace WebApi.Models
{
	public struct FormattedPoint
	{
		/// <summary>
		/// Широта.
		/// </summary>
		public double Latitude { get; set; }

		/// <summary>
		/// Долгота.
		/// </summary>
		public double Longitude { get; set; }

		public static explicit operator Point(FormattedPoint from)
			=> from.ToPoint();

		public static implicit operator FormattedPoint(Point? from)
			=> FormattedPoint.FromPoint(from);

		public Point ToPoint()
			=> new(Longitude, Latitude);

		public static FormattedPoint FromPoint(Point? from)
		{
			if (from is null)
				return default;
			else
				return new() { Longitude = from.X, Latitude = from.Y };
		}
	}
}
