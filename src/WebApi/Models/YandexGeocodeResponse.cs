namespace WebApi.Models
{
	// Содержит не все поля в целях экономии ресурсов.
	public class YandexGeocodeResponse
	{
		public ResponseClass Response { get; set; } = default!;

		// Единственное добавленное поле. Яндекс его не присылает.
		public bool Success { get; set; } = true;

		public class ResponseClass
		{
			public GeoObjectCollection GeoObjectCollection { get; set; } = default!;
		}

		public class GeoObjectCollection
		{
			//public Metadataproperty metaDataProperty { get; set; }
			public Featuremember[] FeatureMember { get; set; } = default!;
		}

		//public class Metadataproperty
		//{
		//	public Geocoderresponsemetadata GeocoderResponseMetaData { get; set; }
		//}

		//public class Geocoderresponsemetadata
		//{
		//	public string request { get; set; }
		//	public string found { get; set; }
		//	public string results { get; set; }
		//}

		public class Featuremember
		{
			public Geoobject GeoObject { get; set; } = default!;
		}

		public class Geoobject
		{
			public Metadataproperty1 MetaDataProperty { get; set; } = default!;
			//public string description { get; set; }
			//public string name { get; set; }
			//public Boundedby boundedBy { get; set; }
			public PointClass Point { get; set; } = default!;
		}

		public class Metadataproperty1
		{
			public GeocoderMetaData GeocoderMetaData { get; set; } = default!;
		}

		public class GeocoderMetaData
		{
			//public string kind { get; set; }
			//public string text { get; set; }
			//public string precision { get; set; }
			public Address2 Address { get; set; } = default!;
			//public Addressdetails AddressDetails { get; set; }
		}

		// Сей нейминг обусловлен тем, что сваггеру не по силам одноимённые классы разрулить.
		public class Address2
		{
			//public string country_code { get; set; }
			//public string postal_code { get; set; }
			public string Formatted { get; set; } = default!;
			public Component[] Components { get; set; } = default!;
		}

		public class Component
		{
			public string Kind { get; set; } = default!;
			public string Name { get; set; } = default!;
		}

		//public class Addressdetails
		//{
		//	public Country Country { get; set; }
		//}

		//public class Country
		//{
		//	public string AddressLine { get; set; }
		//	public string CountryNameCode { get; set; }
		//	public string CountryName { get; set; }
		//	public Administrativearea AdministrativeArea { get; set; }
		//}

		//public class Administrativearea
		//{
		//	public string AdministrativeAreaName { get; set; }
		//	public Locality Locality { get; set; }
		//}

		//public class Locality
		//{
		//	public string LocalityName { get; set; }
		//	public Thoroughfare Thoroughfare { get; set; }
		//}

		//public class Thoroughfare
		//{
		//	public string ThoroughfareName { get; set; }
		//	public Premise Premise { get; set; }
		//}

		//public class Premise
		//{
		//	public string PremiseNumber { get; set; }
		//	public Postalcode PostalCode { get; set; }
		//}

		//public class Postalcode
		//{
		//	public string PostalCodeNumber { get; set; }
		//}

		//public class Boundedby
		//{
		//	public Envelope Envelope { get; set; }
		//}

		//public class Envelope
		//{
		//	public string lowerCorner { get; set; }
		//	public string upperCorner { get; set; }
		//}

		// Чудый нейминг из-за того, что сваггер не может разрулить одноимённые классы.
		public class PointClass
		{
			public string Pos { get; set; } = default!;

			public FormattedPoint ToFormattedPoint()
			{
				if (string.IsNullOrWhiteSpace(Pos))
					return default;

				var span = Pos.AsSpan();

				var spaceIndex = span.IndexOf(' ');

				var lonAsStr = span.Slice(0, spaceIndex);
				var lon = double.Parse(lonAsStr);

				var latAsStr = span.Slice(spaceIndex + 1);
				var lat = double.Parse(latAsStr);

				return new FormattedPoint
				{
					Latitude = lat,
					Longitude = lon,
				};
			}
		}

	}
}
