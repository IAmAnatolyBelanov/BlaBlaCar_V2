namespace WebApi.Shared
{
	public static class Constants
	{
		public static readonly IReadOnlyDictionary<string, (Type Type, string Name)> AllConstants
			= typeof(Constants).GetAllStringConstantsRecursively()
				.ToDictionary(x => x.Value, x => (x.Holder, x.Name));

		public const string DefaultHttpClientName = "Default";

		public const string PostgresMigrationTag = "PostgresMigrationTag";
		public const string PostgresMigratorKey = "PostgresMigratorKey";
	}
}
