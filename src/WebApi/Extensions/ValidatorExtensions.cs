﻿using FluentValidation;

namespace WebApi.Extensions;

public static class ValidatorExtensions
{
	public static void ValidateAndThrowFriendly<T>(this IValidator<T> validator, T instance)
	{
		var validationResult = validator.Validate(instance);
		if (!validationResult.IsValid)
			throw new UserFriendlyException(validationResult);
	}

	public static async Task ValidateAndThrowFriendlyAsync<T>(this IValidator<T> validator, T instance, CancellationToken ct = default)
	{
		var validationResult = await validator.ValidateAsync(instance, ct);
		if (!validationResult.IsValid)
			throw new UserFriendlyException(validationResult);
	}
}
