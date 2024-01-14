using AutoFixture;

using FluentAssertions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Models;
using WebApi.Services.Validators;

namespace Tests
{
	public class LegsValidationTests : IClassFixture<WebApplicationFactory<Program>>
	{

	}
}
