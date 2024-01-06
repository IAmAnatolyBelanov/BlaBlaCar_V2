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
	}
}
