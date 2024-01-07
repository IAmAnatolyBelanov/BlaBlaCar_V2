namespace WebApi.Shared
{
	public static class Constants
	{
		public static readonly IReadOnlyDictionary<string, string> AllConstants
			= typeof(Constants).GetAllStringConstants().ToDictionary();

		public static readonly IReadOnlySet<string> AllConstantValues
			= AllConstants.Values.ToHashSet();

		public const string DefaultHttpClientName = "Default";
	}
}
