using AutoFixture;

using FluentAssertions;

using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Services.Core;
using WebApi.Services.Validators;

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

		//[Fact]
		//public void Ohh()
		//{
		//	var validator = new CustomLegDtoValidator();
		//	var fixture = Shared.BuildDefaultFixture();
		//	var leg = fixture.Create<LegDto>();
		//	var lol = validator.Validate(leg);
		//	Console.WriteLine();
		//}

		//[Fact]
		//public void Ohh2()
		//{
		//	var validator = new CustomLegDtoValidator();
		//	var fixture = Shared.BuildDefaultFixture();
		//	var leg = fixture.Create<LegDto>();
		//	leg.To = leg.From;
		//	leg.PriceInRub = -5;
		//	var lol = validator.Validate(leg);
		//	Console.WriteLine();
		//}

		//[Fact]
		//public void V2_Ohh()
		//{
		//	var validator = new CustomLegDtoValidator_V2();
		//	var fixture = Shared.BuildDefaultFixture();
		//	var leg = fixture.Create<LegDto>();
		//	var lol = validator.Validate(leg);
		//	Console.WriteLine();
		//}
		//[Fact]
		//public void V2_Ohh2()
		//{
		//	var validator = new CustomLegDtoValidator_V2();
		//	var fixture = Shared.BuildDefaultFixture();
		//	var leg = fixture.Create<LegDto>();
		//	leg.To = leg.From;
		//	leg.PriceInRub = -5;
		//	var lol = validator.Validate(x => x.Id.ToString(), leg);
		//	Console.WriteLine();
		//}

		[Fact]
		public void Ooh()
		{
			var validator = new CustomLegDtoValidator_v3();
			var fixture = Shared.BuildDefaultFixture();
			var leg = fixture.Create<LegDto>();
			var lol = validator.Validate(leg);
			Console.WriteLine();
		}

		[Fact]
		public void Ooh_v2()
		{
			var validator = new CustomLegDtoValidator_v3();
			var fixture = Shared.BuildDefaultFixture();
			var leg = fixture.Create<LegDto>();
			leg.To = leg.From;
			leg.PriceInRub = -5;
			var lol = validator.Validate(leg);
			Console.WriteLine();
		}

		[Fact]
		public void Ooh_v3()
		{
			var validator = new CustomLegCollectionValidator_V3(new CustomLegDtoValidator_v3(), new RideServiceConfig());
			var fixture = Shared.BuildDefaultFixture();
			var leg = fixture.Create<LegDto>();
			leg.RideId = leg.Ride.Id;
			//leg.To = leg.From;
			//leg.PriceInRub = -5;
			var lol = validator.Validate([leg]);

			var kek = validator.Validate([]);

			Console.WriteLine();
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
