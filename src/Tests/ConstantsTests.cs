using FluentAssertions;

using WebApi.DataAccess;
using WebApi.Models;

namespace Tests
{
	public class ConstantsTests
	{
		[Fact]
		public void DbConstantsAreUnique()
		{
			var allConstants = DbConstants.AllConstants
				.Concat(DbConstants.IndexNames.AllConstants.AddKeyPrefix("i_"))
				.Concat(DbConstants.FunctionNames.AllConstants.AddKeyPrefix("f_"))
				.Concat(DbConstants.FunctionErrors.AllConstants.AddKeyPrefix("fe_"))
				.Concat(DbConstants.TriggerNames.AllConstants.AddKeyPrefix("t_"))
				.ToDictionary();
			var allConstantValues = DbConstants.AllConstantValues
				.Concat(DbConstants.IndexNames.AllConstantValues)
				.Concat(DbConstants.FunctionNames.AllConstantValues)
				.Concat(DbConstants.FunctionErrors.AllConstantValues)
				.Concat(DbConstants.TriggerNames.AllConstantValues);

			allConstants.Should().HaveSameCount(allConstantValues);
			allConstants.Values.Should().BeEquivalentTo(allConstantValues);
		}


	}

	internal static class DictionaryExtensions
	{
		public static IReadOnlyDictionary<string, string> AddKeyPrefix(this IReadOnlyDictionary<string, string> dictionary, string prefix)
		{
			return dictionary.Select(x => new KeyValuePair<string, string>($"{prefix}{x.Key}", x.Value))
				.ToDictionary();
		}
	}
}
