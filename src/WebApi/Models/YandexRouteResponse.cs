using Newtonsoft.Json;

using System.Text.Json.Serialization;

namespace WebApi.Models
{
	// Содержит не все поля в целях экономии ресурсов.
	public class YandexRouteResponse
	{
		//public string traffic { get; set; }
		public RouteClass Route { get; set; } = default!;

		// Единственное добавленное поле. Яндекс его не присылает.
		public bool Success { get; set; } = true;

		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public TimeSpan TotalDuration { get
			{
				float seconds = 0;

				for (int i = 0; i < Route.Legs.Length; i++)
				{
					var leg = Route.Legs[i];
					for (int j = 0; j < leg.Steps.Length; j++)
					{
						seconds += leg.Steps[j].DurationInSeconds;
					}
				}

				return TimeSpan.FromSeconds(seconds);
			} }

		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public float TotalLengthInMeters { get
			{
				float length = 0;

				for (int i = 0; i < Route.Legs.Length; i++)
				{
					var leg = Route.Legs[i];
					for (int j = 0; j < leg.Steps.Length; j++)
					{
						length += leg.Steps[j].LengthInMeters;
					}
				}

				return length;
			}
		}

		public class RouteClass
		{
			public YandexLeg[] Legs { get; set; } = default!;
			//public Flags Flags { get; set; }
		}

		//public class Flags
		//{
		//	public bool HasTolls { get; set; }
		//	public bool HasNonTransactionalTolls { get; set; }
		//}

		public class YandexLeg
		{
			public string Status { get; set; } = default!;
			public Step[] Steps { get; set; } = default!;
		}

		public class Step
		{
			[JsonPropertyName("duration")]
			[JsonProperty("duration")]
			public float DurationInSeconds { get; set; }
			[JsonPropertyName("length")]
			[JsonProperty("length")]
			public float LengthInMeters { get; set; }
			public Polyline Polyline { get; set; } = default!;
			//public string feature_class { get; set; }
			//public string mode { get; set; }
			//public int waiting_duration { get; set; }
		}

		public class Polyline
		{
			public double[][] Points { get; set; } = default!;

			public FormattedPoint[] ToFormattedPoints()
			{
				var result = new FormattedPoint[Points.Length];
				for (int i = 0; i < Points.Length; i++)
				{
					result[i] = new FormattedPoint
					{
						Latitude = Points[i][0],
						Longitude = Points[i][1],
					};
				}
				return result;
			}
		}

	}
}
