using System.Reflection;

namespace WebApi.Extensions
{
	public static class TypeExtensions
	{
		public static IEnumerable<(Type Holder, string Name, string Value)> GetAllStringConstants<T>()
			=> GetAllStringConstants(typeof(T));

		public static IEnumerable<(Type Holder, string Name, string Value)> GetAllStringConstants(this Type type)
		{
			var fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

			var constantNames = fieldInfos
				.Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
				.Select(fi => (type, fi.Name, (string)fi.GetValue(null)!));

			return constantNames;
		}

		public static IEnumerable<(Type Holder, string Name, string Value)> GetAllStringConstantsRecursively<T>()
			=> GetAllStringConstantsRecursively(typeof(T));

		public static IEnumerable<(Type Holder, string Name, string Value)> GetAllStringConstantsRecursively(this Type type)
		{
			var firstLevel = GetAllStringConstants(type);

			foreach (var constant in firstLevel)
				yield return constant;

			var assembly = type.Assembly;
			var children = assembly.GetTypes()
				.Where(x => type.IsAssignableFrom(x) && x != type)
				.ToArray();

			var result = children
				.SelectMany(GetAllStringConstantsRecursively);

			foreach (var constant in result)
				yield return constant;
		}
	}
}
