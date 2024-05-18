using Newtonsoft.Json;

namespace WebApi.Models.DriverServiceModels;

public class VinDecoderResponse
{
	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("found")]
	public bool Found { get; set; }

	[JsonProperty("VIN")]
	public TitleValuePair? Vin { get; set; }

	[JsonProperty("WMI")]
	public TitleValuePair? Wmi { get; set; }

	[JsonProperty("VIS identifier")]
	public TitleValuePair? VisIdentifier { get; set; }

	[JsonProperty("VDS")]
	public TitleValuePair? Vds { get; set; }

	[JsonProperty("Year_identifier")]
	public TitleValuePair? YearIdentifier { get; set; }

	[JsonProperty("Serial_number")]
	public TitleValuePair? SerialNumber { get; set; }

	[JsonProperty("VIN_type")]
	public TitleValuePair? VinType { get; set; }

	[JsonProperty("Make")]
	public TitleValuePair? Make { get; set; }

	[JsonProperty("Model")]
	public TitleValuePair? Model { get; set; }

	[JsonProperty("Year")]
	public TitleValuePair? Year { get; set; }

	[JsonProperty("Body")]
	public TitleValuePair? Body { get; set; }

	[JsonProperty("Engine")]
	public TitleValuePair? Engine { get; set; }

	[JsonProperty("Fuel")]
	public TitleValuePair? Fuel { get; set; }

	[JsonProperty("Transmission")]
	public TitleValuePair? Transmission { get; set; }

	[JsonProperty("classCar")]
	public TitleValuePair? ClassCar { get; set; }

	[JsonProperty("typeCar")]
	public TitleValuePair? TypeCar { get; set; }

	[JsonProperty("Manufactured")]
	public TitleValuePair? Manufactured { get; set; }

	[JsonProperty("Body_type")]
	public TitleValuePair? BodyType { get; set; }

	[JsonProperty("Number_doors")]
	public TitleValuePair? NumberDoors { get; set; }

	[JsonProperty("Number_seats")]
	public TitleValuePair? NumberSeats { get; set; }

	[JsonProperty("Displacement")]
	public TitleValuePair? Displacement { get; set; }

	[JsonProperty("Displacement_nominal")]
	public TitleValuePair? DisplacementNominal { get; set; }

	[JsonProperty("Engine_valves")]
	public TitleValuePair? EngineValves { get; set; }

	[JsonProperty("cylinders")]
	public TitleValuePair? Cylinders { get; set; }

	[JsonProperty("gearbox")]
	public TitleValuePair? Gearbox { get; set; }

	[JsonProperty("HorsePower")]
	public TitleValuePair? HorsePower { get; set; }

	[JsonProperty("KiloWatts")]
	public TitleValuePair? KiloWatts { get; set; }

	[JsonProperty("Emission_standard")]
	public TitleValuePair? EmissionStandard { get; set; }

	[JsonProperty("Driveline")]
	public TitleValuePair? Driveline { get; set; }

	[JsonProperty("ABS")]
	public TitleValuePair? Abs { get; set; }

	[JsonProperty("Manufacturer")]
	public TitleValuePair? Manufacturer { get; set; }

	[JsonProperty("Adress1")]
	public TitleValuePair? Adress1 { get; set; }

	[JsonProperty("Adress2")]
	public TitleValuePair? Adress2 { get; set; }

	[JsonProperty("Region")]
	public TitleValuePair? Region { get; set; }

	[JsonProperty("Country")]
	public TitleValuePair? Country { get; set; }

	[JsonProperty("Note")]
	public TitleValuePair? Note { get; set; }

	[JsonProperty("Standard_equipment")]
	public TitleValuesPair? StandardEquipment { get; set; }

	[JsonProperty("Optional_equipment")]
	public TitleValuesPair? OptionalEquipment { get; set; }

	[JsonProperty("logo")]
	public TitleValuePair? Logo { get; set; }

	[JsonProperty("inquiry")]
	public Inquiry Inquiry { get; set; } = default!;


	public class TitleValuePair
	{
		[JsonProperty("title")]
		public string Title { get; set; } = default!;

		[JsonProperty("value")]
		public string? Value { get; set; }
	}

	public class TitleValuesPair
	{
		[JsonProperty("title")]
		public string Title { get; set; } = default!;

		[JsonProperty("value")]
		public string[]? Value { get; set; }
	}
}
