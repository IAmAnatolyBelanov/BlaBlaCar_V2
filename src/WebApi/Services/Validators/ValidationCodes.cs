namespace WebApi.Services.Validators
{
	public class CommonValidationCodes : ValidationCodes
	{
		public const string UserNotFound = "CommonErrors_UserNotFound";
		public const string CarNotFound = "CommonErrors_CarNotFound";
		public const string RideNotFound = "CommonErrors_RideNotFound";
	}

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
