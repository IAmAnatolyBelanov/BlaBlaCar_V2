using System.Reflection;

namespace WebApi.Extensions
{
	public static class TypeExtensions
	{
		public static IEnumerable<KeyValuePair<string, string>> GetAllStringConstants(this Type type)
		{
			var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

			var constantNames = fieldInfos
				.Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
				.Select(fi => new KeyValuePair<string, string>(fi.Name, (string)fi.GetValue(null)!));

			return constantNames;
		}
	}
}
