﻿using NetTopologySuite.Geometries;

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
	}
}
