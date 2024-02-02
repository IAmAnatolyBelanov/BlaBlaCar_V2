using NetTopologySuite.Geometries;

namespace WebApi.Models
{
	public readonly struct FormattedPoint
	{
		/// <summary>
		/// Широта. [-90; +90].
		/// </summary>
		public double Latitude { get; init; }

		/// <summary>
		/// Долгота. [-180; +180].
		/// </summary>
		public double Longitude { get; init; }

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

		public static bool operator ==(FormattedPoint a, FormattedPoint b)
			=> a.Latitude == b.Latitude && a.Longitude == b.Longitude;
		public static bool operator !=(FormattedPoint a, FormattedPoint b)
			=> !(a == b);

		public override bool Equals(object? obj)
		{
			if (obj == null) return false;
			if (obj is not FormattedPoint other) return false;
			return this == other;
		}

		public override int GetHashCode()
			=> HashCode.Combine(Longitude, Latitude);

		public override string ToString()
			=> $"{{\"{nameof(Latitude)}\":{Latitude},\"{nameof(Longitude)}\":{Longitude}}}";
	}
}
