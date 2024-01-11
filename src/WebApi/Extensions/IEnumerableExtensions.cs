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

		public static T[] ToArray<T>(this IEnumerable<T> values, int capasity)
		{
			if (capasity == 0)
				return Array.Empty<T>();

			var result = new T[capasity];

			int iterator = 0;
			foreach (var value in values)
			{
				result[iterator] = value;
				iterator++;
			}
			return result;
		}

		public static bool TrySort<T>(this IReadOnlyCollection<T> collection, Comparison<T> comparison)
		{
			if (collection is T[] array)
			{
				Array.Sort(array, comparison);
				return true;
			}
			if (collection is List<T> list)
			{
				list.Sort(comparison);
				return true;
			}

			return false;
		}
	}
}
