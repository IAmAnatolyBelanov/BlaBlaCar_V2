using NetTopologySuite.Geometries;

using System.Diagnostics;

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

		public static implicit operator FormattedPoint(Point from)
			=> new() { Longitude = from.X, Latitude = from.Y };

		public Point ToPoint()
			=> new(Longitude, Latitude);
	}
}
