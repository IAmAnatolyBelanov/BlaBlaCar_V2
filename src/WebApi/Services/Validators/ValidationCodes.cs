namespace WebApi.Services.Validators
{
	public abstract class ValidationCodes
	{
		public ValidationCodes()
		{
			throw new NotSupportedException();
		}

		public static readonly IReadOnlyDictionary<string, (Type Type, string Name)> AllConstants
			= typeof(ValidationCodes).GetAllStringConstantsRecursively()
				.ToDictionary(x => x.Value, x => (x.Holder, x.Name));
	}
}
