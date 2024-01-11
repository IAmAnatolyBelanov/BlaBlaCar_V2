using System.Diagnostics.CodeAnalysis;

namespace WebApi.Extensions
{
	public static class NullChecker
	{
		public static void Check<T0>([NotNull] T0? value0)
		{
			ArgumentNullException.ThrowIfNull(value0);
		}

		public static void Check<T0, T1>([NotNull] T0? value0, [NotNull] T1? value1)
		{
			ArgumentNullException.ThrowIfNull(value0);
			ArgumentNullException.ThrowIfNull(value1);
		}

		public static void Check([NotNull] params object[] args)
		{
			ArgumentNullException.ThrowIfNull(args);

			foreach (object arg in args)
			{
				ArgumentNullException.ThrowIfNull(arg);
			}
		}
	}
}
