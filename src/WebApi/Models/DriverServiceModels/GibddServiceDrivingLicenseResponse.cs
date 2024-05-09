using Newtonsoft.Json;

namespace WebApi.Models.DriverServiceModels;

public class GibddServiceDrivingLicenseResponse
{
	[JsonProperty("doc")]
	public DocClass Doc { get; set; } = default!;

	/// <summary>
	/// Информация о лишениях.
	/// </summary>
	[JsonProperty("decis")]
	public Deci[]? Decis { get; set; }

	[JsonProperty("status")]
	public int Status { get; set; }

	[JsonProperty("found")]
	public bool Found { get; set; }

	[JsonProperty("inquiry")]
	public Inquiry Inquiry { get; set; } = default!;


	/// <summary>
	/// Информация о лишениях.
	/// </summary>
	public class Deci
	{
		[JsonProperty("date")]
		public DateTimeOffset Date { get; set; }

		[JsonProperty("fis_id")]
		public string FisId { get; set; }

		[JsonProperty("bplace")]
		public string Bplace { get; set; }

		[JsonProperty("comment")]
		public string Comment { get; set; }

		[JsonProperty("reg_name")]
		public string RegName { get; set; }

		[JsonProperty("state")]
		public string State { get; set; }

		/// <summary>
		/// Срок лишения (в месяцах).
		/// </summary>
		[JsonProperty("srok")]
		public int Srok { get; set; }

		[JsonProperty("reg_code")]
		public string RegCode { get; set; }

		[JsonProperty("stateinfo")]
		public string StateInfo { get; set; }
	}

	public class DocClass
	{
		/// <summary>
		/// Кем выдано.
		/// </summary>
		[JsonProperty("division")]
		public string Division { get; set; }

		/// <summary>
		/// Дата выдачи.
		/// </summary>
		[JsonProperty("date")]
		public DateTimeOffset Date { get; set; }

		/// <summary>
		/// Стаж с.
		/// </summary>
		[JsonProperty("stag")]
		public string Stag { get; set; }

		/// <summary>
		/// Дата рождения.
		/// </summary>
		[JsonProperty("bdate")]
		public DateTimeOffset Bdate { get; set; }

		/// <summary>
		/// Серия и номер.
		/// </summary>
		[JsonProperty("num")]
		public string Num { get; set; }

		/// <summary>
		/// Категории ТС через запятую.
		/// </summary>
		/// <remarks>
		/// Может содержать как русские, так и английские буквы. Перед использованием нужно всё маппить в английские.
		/// </remarks>
		[JsonProperty("cat")]
		public string Cat { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		/// <summary>
		/// Действителен до.
		/// </summary>
		[JsonProperty("srok")]
		public DateTimeOffset Srok { get; set; }

		[JsonProperty("divid")]
		public string Divid { get; set; }
	}
}