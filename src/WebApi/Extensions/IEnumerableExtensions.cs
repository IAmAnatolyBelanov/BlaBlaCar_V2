namespace WebApi.Extensions
{
	public static class IEnumerableExtensions
	{
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> action)
			where T : class
		{
			foreach (var item in collection)
			{
				action(item);
				yield return item;
			}
		}

		public static T[] ToArray<T>(this IEnumerable<T> values, int capacity)
		{
			if (capacity == 0)
				return Array.Empty<T>();

			var result = new T[capacity];

			int iterator = 0;
			foreach (var value in values)
			{
				result[iterator] = value;
				iterator++;
			}

			return result;
		}

		public static bool ContainsNull<T>(this IReadOnlyList<T> values)
			where T : class
		{
			for (int i = 0; i < values.Count; i++)
				if (values[i] is null)
					return true;

			return false;
		}

		public static bool TrueForAll<T>(this IReadOnlyList<T> values, Func<T, bool> condition)
		{
			for (int i = 0; i < values.Count; i++)
			{
				if (!condition(values[i]))
					return false;
			}
			return true;
		}
	}
}
