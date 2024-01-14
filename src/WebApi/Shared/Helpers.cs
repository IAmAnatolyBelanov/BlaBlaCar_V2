namespace WebApi.Shared
{
	public static class Helpers
	{
		public static IEnumerable<(int PricesCount, int WaypointsCount)> ValidPriceWaypointCounts(int maxWaypoints)
		{
			var lastValidPricesCount = 1;
			yield return (lastValidPricesCount, 2);

			for (int waypointCount = 3; waypointCount <= maxWaypoints; waypointCount++)
			{
				lastValidPricesCount += waypointCount - 1;
				yield return (lastValidPricesCount, waypointCount);
			}
		}
	}
}
