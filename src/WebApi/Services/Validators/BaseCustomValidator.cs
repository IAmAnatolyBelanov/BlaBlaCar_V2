//using Newtonsoft.Json;

//namespace WebApi.Services.Validators
//{
//	//public abstract class CustomAbstractValidator_V2<T> : AbstractValidator<T>
//	//{
//	//	private class IdentifiedValidator : AbstractValidator<(Func<T, string> IdentifierGetter, T Instance)>
//	//	{
//	//		public IdentifiedValidator(IValidator<T> validator)
//	//		{
//	//			RuleFor(x => x.Instance)
//	//				.SetValidator(validator)
//	//				.WithMessage(t => $"Fail validation on {t.IdentifierGetter(t.Instance)}");
//	//		}
//	//	}
//	//	private readonly IdentifiedValidator _identifiedValidator;

//	//	public CustomAbstractValidator_V2()
//	//	{
//	//		_identifiedValidator = new IdentifiedValidator(this);
//	//	}

//	//	public ValidationResult Validate(Func<T, string> identifierGetter, T instance)
//	//	{
//	//		return _identifiedValidator.Validate((identifierGetter, instance));
//	//	}
//	//}



//	public struct CustomValidationResult
//	{
//		public bool IsValid => Errors is null || Errors.Count == 0;

//		public List<CustomValidationFailure>? Errors { get; set; }
//	}

//	public class CustomValidationFailure
//	{
//		public string? ErrorCode { get; set; }
//		public string? ErrorMessage { get; set; }
//		public string? PropertyName { get; set; }
//		public string? AttemptedObjectIdentifier { get; set; }
//		public string? AdditionalInfo { get; set; }
//		public CustomRule Rule { get; set; } = default!;
//		public IReadOnlyList<CustomValidationFailure>? Internal { get; set; }

//		public CustomValidationFailure() { }

//		public CustomValidationFailure(
//			string errorCode,
//			string errorMessage,
//			string propertyName,
//			string attemptedObjectIdentifier,
//			string? additionalInfo,
//			CustomRule rule,
//			IReadOnlyList<CustomValidationFailure>? @internal)
//		{
//			ErrorCode = errorCode;
//			ErrorMessage = errorMessage;
//			PropertyName = propertyName;
//			AttemptedObjectIdentifier = attemptedObjectIdentifier;
//			AdditionalInfo = additionalInfo;
//			Rule = rule;
//			Internal = @internal;
//		}
//	}

//	public abstract class AbstractBaseCollectionValidator<T> : ICustomValidator<IEnumerable<T>>
//	{
//		protected readonly List<CustomRule> _rules = new();

//		public virtual CustomValidationResult Validate(IEnumerable<T> instances)
//			=> Validate(instances, null, null);

//		public virtual CustomValidationResult Validate(IEnumerable<T> instances, List<CustomValidationFailure>? errors, HashSet<CustomRule>? failedRules)
//		{
//			var list = instances as IReadOnlyList<T>;

//			for (int i = 0; i < _rules.Count; i++)
//			{
//				var rule = _rules[i];

//				if (rule is CustomRule<IEnumerable<T>> ruleForMany)
//				{
//					var error = ruleForMany.Validate(instances, errors, failedRules, default);
//					if (error is not null)
//					{
//						if (errors is null)
//							errors = new();
//						if (failedRules is null)
//							failedRules = new();

//						errors.Add(error);
//						failedRules.Add(error.Rule);
//					}
//					continue;
//				}

//				if (rule is CustomRule<T> ruleForOne)
//				{
//					if (list is not null)
//					{
//						for (int j = 0; j < list.Count; j++)
//						{
//							var instance = list[j];
//							var error = ruleForOne.Validate(instance, errors, failedRules, j);
//							if (error is not null)
//							{
//								if (errors is null)
//									errors = new();
//								if (failedRules is null)
//									failedRules = new();

//								errors.Add(error);
//								failedRules.Add(error.Rule);
//							}
//						}
//					}
//					else
//					{
//						int index = 0;
//						foreach (var instance in instances)
//						{
//							var error = ruleForOne.Validate(instance, errors, failedRules, index);
//							if (error is not null)
//							{
//								if (errors is null)
//									errors = new();
//								if (failedRules is null)
//									failedRules = new();

//								errors.Add(error);
//								failedRules.Add(error.Rule);
//							}
//							index++;
//						}
//					}
//				}
//			}

//			return new CustomValidationResult
//			{
//				Errors = errors
//			};
//		}

//		public virtual async ValueTask<CustomValidationResult> ValidateAsync(IEnumerable<T> instances)
//			=> await ValidateAsync(instances, null, null);

//		public virtual async ValueTask<CustomValidationResult> ValidateAsync(IEnumerable<T> instances, List<CustomValidationFailure>? errors, HashSet<CustomRule>? failedRules)
//		{

//			var list = instances as IReadOnlyList<T>;

//			for (int i = 0; i < _rules.Count; i++)
//			{
//				var rule = _rules[i];

//				if (rule is CustomRule<IEnumerable<T>> ruleForMany)
//				{
//					var error = await ruleForMany.ValidateAsync(instances, errors, failedRules, default);
//					if (error is not null)
//					{
//						if (errors is null)
//							errors = new();
//						if (failedRules is null)
//							failedRules = new();

//						errors.Add(error);
//						failedRules.Add(error.Rule);
//					}
//					continue;
//				}

//				if (rule is CustomRule<T> ruleForOne)
//				{
//					if (list is not null)
//					{
//						for (int j = 0; j < list.Count; j++)
//						{
//							var instance = list[j];
//							var error = await ruleForOne.ValidateAsync(instance, errors, failedRules, j);
//							if (error is not null)
//							{
//								if (errors is null)
//									errors = new();
//								if (failedRules is null)
//									failedRules = new();

//								errors.Add(error);
//								failedRules.Add(error.Rule);
//							}
//						}
//					}
//					else
//					{
//						int index = 0;
//						foreach (var instance in instances)
//						{
//							var error = await ruleForOne.ValidateAsync(instance, errors, failedRules, index);
//							if (error is not null)
//							{
//								if (errors is null)
//									errors = new();
//								if (failedRules is null)
//									failedRules = new();

//								errors.Add(error);
//								failedRules.Add(error.Rule);
//							}
//							index++;
//						}
//					}
//				}
//			}

//			return new CustomValidationResult
//			{
//				Errors = errors
//			};
//		}

//		public abstract void ValidateAndThrow(IEnumerable<T> instances);
//		public abstract ValueTask ValidateAndThrowAsync(IEnumerable<T> instances);

//		protected virtual CustomRule<IEnumerable<T>> AddRule(Func<IEnumerable<T>, CustomValidationFailure?> validator, params CustomRule[] necessaryRules)
//			=> AddRule((t, errors, failedRules) => validator(t), necessaryRules);

//		protected virtual CustomRule<IEnumerable<T>> AddRule(Func<IEnumerable<T>, List<CustomValidationFailure>?, HashSet<CustomRule>?, CustomValidationFailure?> validator, params CustomRule[] necessaryRules)
//		{
//			var result = new CustomRule<IEnumerable<T>>();

//			result.Validate = (t, errors, failedRules, index) =>
//			{
//				if(necessaryRules is not null)
//				{
//					for (int i = 0; i <  necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return null;
//						}
//					}
//				}

//				var error = validator(t, errors, failedRules)!;
//				if (error is not null)
//					error.Rule = result;
//				return error;
//			};
//			result.ValidateAsync = (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return ValueTask.FromResult(default(CustomValidationFailure?));
//						}
//					}
//				}

//				var error = validator(t, errors, failedRules)!;
//				if (error is not null)
//					error.Rule = result;
//				return new(error);
//			};

//			_rules.Add(result);
//			return result;
//		}

//		protected virtual CustomRule<IEnumerable<T>> AddRule(Func<IEnumerable<T>, int?, ValueTask<CustomValidationFailure?>> validator, params CustomRule[] necessaryRules)
//			=> AddRule((t, errors, failedRules, index) => validator(t, index), necessaryRules);

//		protected virtual CustomRule<IEnumerable<T>> AddRule(Func<IEnumerable<T>, List<CustomValidationFailure>?, HashSet<CustomRule>?, int?, ValueTask<CustomValidationFailure?>> validator, params CustomRule[] necessaryRules)
//		{
//			var result = new CustomRule<IEnumerable<T>>();

//			result.Validate = (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return null;
//						}
//					}
//				}

//				var error = Task.Run(async () => await validator(t, errors, failedRules, index)).Result!;
//				if (error is not null)
//					error.Rule = result;
//				return error;
//			};
//			result.ValidateAsync = async (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return null;
//						}
//					}
//				}

//				var error = await validator(t, errors, failedRules, index)!;
//				if (error is not null)
//					error.Rule = result;
//				return error;
//			};

//			_rules.Add(result);
//			return result;
//		}


//		protected virtual CustomRule<T> AddRuleForeach(Func<T, int?, CustomValidationFailure?> validator, params CustomRule[] necessaryRules)
//			=> AddRuleForeach((t, errors, failedRules, index) => validator(t, index), necessaryRules);

//		protected virtual CustomRule<T> AddRuleForeach(Func<T, List<CustomValidationFailure>?, HashSet<CustomRule>?, int?, CustomValidationFailure?> validator, params CustomRule[] necessaryRules)
//		{
//			var result = new CustomRule<T>();

//			result.Validate = (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return null;
//						}
//					}
//				}

//				var error = validator(t, errors, failedRules, index)!;
//				if (error is not null)
//					error.Rule = result;
//				return error;
//			};
//			result.ValidateAsync = (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return ValueTask.FromResult(default(CustomValidationFailure?));
//						}
//					}
//				}

//				var error = validator(t, errors, failedRules, index)!;
//				if (error is not null)
//					error.Rule = result;
//				return new(error);
//			};

//			_rules.Add(result);
//			return result;
//		}

//		protected virtual CustomRule<T> AddRuleForeach(Func<T, int?, ValueTask<CustomValidationFailure?>> validator, params CustomRule[] necessaryRules)
//			=> AddRuleForeach((t, errors, failedRules, index) => validator(t, index), necessaryRules);

//		protected virtual CustomRule<T> AddRuleForeach(Func<T, List<CustomValidationFailure>?, HashSet<CustomRule>?, int?, ValueTask<CustomValidationFailure?>> validator, params CustomRule[] necessaryRules)
//		{
//			var result = new CustomRule<T>();

//			result.Validate = (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return null;
//						}
//					}
//				}

//				var error = Task.Run(async () => await validator(t, errors, failedRules, index)).Result!;
//				if (error is not null)
//					error.Rule = result;
//				return error;
//			};
//			result.ValidateAsync = async (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return null;
//						}
//					}
//				}

//				var error = await validator(t, errors, failedRules, index);
//				if (error is not null)
//					error.Rule = result;
//				return error;
//			};

//			_rules.Add(result);
//			return result;
//		}
//	}

//	public abstract class AbstractBaseValidator<T> : ICustomValidator<T>
//	{
//		protected readonly List<CustomRule<T>> _rules = new();

//		public virtual CustomValidationResult Validate(T instance)
//			=> Validate(instance, null, null);

//		public virtual CustomValidationResult Validate(T instance, List<CustomValidationFailure>? errors, HashSet<CustomRule>? failedRules)
//		{
//			for (int i = 0; i < _rules.Count; i++)
//			{
//				var error = _rules[i].Validate(instance, errors, failedRules, default);
//				if (error is not null)
//				{
//					if (errors is null)
//						errors = new();
//					if (failedRules is null)
//						failedRules = new();

//					errors.Add(error);
//					failedRules.Add(error.Rule);
//				}
//			}

//			return new CustomValidationResult
//			{
//				Errors = errors
//			};
//		}

//		public virtual async ValueTask<CustomValidationResult> ValidateAsync(T instance)
//			=> await ValidateAsync(instance, null, null);

//		public virtual async ValueTask<CustomValidationResult> ValidateAsync(T instance, List<CustomValidationFailure>? errors, HashSet<CustomRule>? failedRules)
//		{
//			for (int i = 0; i < _rules.Count; i++)
//			{
//				var error = await _rules[i].ValidateAsync(instance, errors, failedRules, default);
//				if (error is not null)
//				{
//					if (errors is null)
//						errors = new();
//					if (failedRules is null)
//						failedRules = new();

//					errors.Add(error);
//					failedRules.Add(error.Rule);
//				}
//			}

//			return new CustomValidationResult
//			{
//				Errors = errors
//			};
//		}

//		public abstract void ValidateAndThrow(T instance);
//		public abstract ValueTask ValidateAndThrowAsync(T instance);

//		protected virtual CustomRule<T> AddRule(Func<T, CustomValidationFailure?> validator, params CustomRule[] necessaryRules)
//			=> AddRule((t, errors, failedRules) => validator(t), necessaryRules);

//		protected virtual CustomRule<T> AddRule(Func<T, IReadOnlyList<CustomValidationFailure>?, IReadOnlySet<CustomRule>?, CustomValidationFailure?> validator, params CustomRule[] necessaryRules)
//		{
//			var result = new CustomRule<T>();

//			result.Validate = (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return null;
//						}
//					}
//				}

//				var error = validator(t, errors, failedRules)!;
//				if (error is not null)
//					error.Rule = result;
//				return error;
//			};
//			result.ValidateAsync = (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return ValueTask.FromResult(default(CustomValidationFailure?));
//						}
//					}
//				}

//				var error = validator(t, errors, failedRules)!;
//				if (error is not null)
//					error.Rule = result;
//				return new(error);
//			};

//			_rules.Add(result);
//			return result;
//		}

//		protected virtual CustomRule<T> AddRule(Func<T, int?, ValueTask<CustomValidationFailure?>> validator, params CustomRule[] necessaryRules)
//			=> AddRule((t, errors, failedRules, index) => validator(t, index), necessaryRules);

//		protected virtual CustomRule<T> AddRule(Func<T, List<CustomValidationFailure>?, HashSet<CustomRule>?, int?, ValueTask<CustomValidationFailure?>> validator, params CustomRule[] necessaryRules)
//		{
//			var result = new CustomRule<T>();

//			result.Validate = (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return null;
//						}
//					}
//				}

//				var error = Task.Run(async () => await validator(t, errors, failedRules, index)).Result!;
//				if (error is not null)
//					error.Rule = result;
//				return error;
//			};
//			result.ValidateAsync = async (t, errors, failedRules, index) =>
//			{
//				if (necessaryRules is not null)
//				{
//					for (int i = 0; i < necessaryRules.Length; i++)
//					{
//						if (failedRules?.Contains(necessaryRules[i]) == true)
//						{
//							return null;
//						}
//					}
//				}

//				var error = await validator(t, errors, failedRules, index);
//				if (error is not null)
//					error.Rule = result;
//				return error;
//			};

//			_rules.Add(result);
//			return result;
//		}
//	}

//	public interface ICustomValidator<T>
//	{
//		CustomValidationResult Validate(T instance);
//		ValueTask<CustomValidationResult> ValidateAsync(T instance);
//		void ValidateAndThrow(T instance);
//		ValueTask ValidateAndThrowAsync(T instance);
//		CustomValidationResult Validate(T instance, List<CustomValidationFailure>? errors, HashSet<CustomRule>? failedRules);
//		ValueTask<CustomValidationResult> ValidateAsync(T instance, List<CustomValidationFailure>? errors, HashSet<CustomRule>? failedRules);
//	}

//	public class CustomRule<T> : CustomRule
//	{
//		public Func<T, List<CustomValidationFailure>?, HashSet<CustomRule>?, int?, ValueTask<CustomValidationFailure?>> ValidateAsync { get; set; } = default!;
//		public Func<T, List<CustomValidationFailure>?, HashSet<CustomRule>?, int?, CustomValidationFailure?> Validate { get; set; } = default!;
//	}
//	public abstract class CustomRule { }

//	public class CustomValidationException : Exception
//	{
//		public IReadOnlyList<CustomValidationFailure> Errors { get; init; }

//		public CustomValidationException(CustomValidationResult validationResult)
//		{
//			Errors = validationResult.Errors!;
//		}

//		public override string Message => JsonConvert.SerializeObject(Errors);
//	}


//	//public abstract class CustomAbstractValidator<T> : ICustomValidator<T>
//	//{
//	//	private readonly List<CustomRule<T>> _rules = new();

//	//	public RuleBuilder<T> StartRule(Func<T, bool> validation)
//	//		=> new RuleBuilder<T>(_rules)
//	//			.SetValidation(validation);

//	//	public RuleBuilder<T> StartRule(Func<T, ValueTask<bool>> validation)
//	//		=> new RuleBuilder<T>(_rules)
//	//			.SetValidation(validation);

//	//	public RuleBuilder<T> StartRuleForEach<TItem>(Func<T, IReadOnlyList<TItem>> collectionGetter, Func<TItem, bool> validation)
//	//	{
//	//		return new RuleBuilder<T>(_rules)
//	//			.SetValidation(t =>
//	//			{
//	//				var collection = collectionGetter(t);

//	//				for (var i = 0; i < collection.Count; i++)
//	//				{
//	//					if (!validation(collection[i]))
//	//						return false;
//	//				}

//	//				return true;
//	//			});
//	//	}

//	//	public RuleBuilder<T> StartRuleForEach<TItem>(Func<T, IReadOnlyList<TItem>> collectionGetter, Func<TItem, ValueTask<bool>> validation)
//	//	{
//	//		return new RuleBuilder<T>(_rules)
//	//			.SetValidation(async t =>
//	//			{
//	//				var collection = collectionGetter(t);

//	//				for (var i = 0; i < collection.Count; i++)
//	//				{
//	//					if (!await validation(collection[i]))
//	//						return false;
//	//				}

//	//				return true;
//	//			});
//	//	}


//	//	public CustomValidationResult Validate(T instance)
//	//	{
//	//		List<CustomValidationFailure>? failures = null;

//	//		for (int i = 0; i < _rules.Count; i++)
//	//		{
//	//			var fail = _rules[i].Validate(instance, failures);
//	//			if (fail is not null)
//	//			{
//	//				if (failures is null)
//	//					failures = new();

//	//				failures.Add(fail);
//	//			}
//	//		}

//	//		return new CustomValidationResult
//	//		{
//	//			Errors = failures
//	//		};
//	//	}

//	//	public CustomValidationResult Validate(Func<T, string> getIdentifier, T instance) => throw new NotImplementedException();
//	//	public void ValidateAndThrow(T instance) => throw new NotImplementedException();
//	//	public void ValidateAndThrow(Func<T, string> getIdentifier, T instance) => throw new NotImplementedException();
//	//	public ValueTask ValidateAndThrowAsync(T instance) => throw new NotImplementedException();
//	//	public ValueTask ValidateAndThrowAsync(Func<T, string> getIdentifier, T instance) => throw new NotImplementedException();
//	//	public ValueTask<CustomValidationResult> ValidateAsync(T instance) => throw new NotImplementedException();
//	//	public ValueTask<CustomValidationResult> ValidateAsync(Func<T, string> getIdentifier, T instance) => throw new NotImplementedException();
//	//}

//	//public class RuleBuilder<T>
//	//{
//	//	protected readonly CustomRule<T> _rule = default!;
//	//	protected Func<ValidationContext<T>, CustomValidationFailure> _failureFactory = default!;
//	//	protected Func<ValidationContext<T>, IReadOnlyList<CustomValidationFailure>?, ValueTask<bool>> _validateAsync = default!;
//	//	protected Func<ValidationContext<T>, IReadOnlyList<CustomValidationFailure>?, bool> _validate = default!;
//	//	protected readonly List<CustomRule<T>> _rules = default!;

//	//	private RuleBuilder() { }

//	//	public RuleBuilder(List<CustomRule<T>> rules)
//	//	{
//	//		_rules = rules;
//	//		_rule = new();
//	//	}

//	//	public virtual RuleBuilder<T> SetValidation(Func<T, bool> validation)
//	//	{
//	//		_failureFactory = (t) => new() { Rule = _rule, };
//	//		_validate = (t, results) => validation(t.Instance);
//	//		_validateAsync = (t, results) => new(validation(t.Instance));

//	//		return this;
//	//	}

//	//	public virtual RuleBuilder<T> SetValidation(Func<T, ValueTask<bool>> validation)
//	//	{
//	//		_failureFactory = (t) => new() { Rule = _rule, };
//	//		_validate = (t, results) => Task.Run(async () => await validation(t.Instance)).Result;
//	//		_validateAsync = async (t, results) => await validation(t.Instance);

//	//		return this;
//	//	}

//	//	public virtual RuleBuilder<T> OnlyIfSuccess(params CustomRule<T>[] rules)
//	//	{
//	//		var settedRules = rules.ToHashSet<object>();
//	//		Func<T, IReadOnlyList<CustomValidationFailure>?, bool> predicate = (t, failedResults) =>
//	//		{
//	//			if (failedResults == null || failedResults.Count == 0)
//	//				return true;

//	//			for (int i = 0; i < failedResults.Count; i++)
//	//			{
//	//				if (settedRules.Contains(failedResults[i].Rule))
//	//					return false;
//	//			}

//	//			return true;
//	//		};

//	//		AddPredicate(predicate);

//	//		return this;
//	//	}

//	//	public virtual RuleBuilder<T> AddPredicate(Func<T, bool> predicate)
//	//		=> AddPredicate((t, results) => predicate(t));

//	//	public virtual RuleBuilder<T> AddPredicate(Func<T, IReadOnlyList<CustomValidationFailure>?, bool> predicate)
//	//	{
//	//		if (_rule.Predicate == null)
//	//		{
//	//			_rule.Predicate = (t, tCollection) => predicate(t.Instance, tCollection);
//	//		}
//	//		else
//	//		{
//	//			var storedPredicate = _rule.Predicate;
//	//			_rule.Predicate = (t, results)
//	//				=> storedPredicate(t, results) && predicate(t.Instance, results);
//	//		}
//	//		return this;
//	//	}

//	//	public virtual RuleBuilder<T> SetErrorCode(string code)
//	//		=> SetErrorCode(() => code);

//	//	public virtual RuleBuilder<T> SetErrorCode(Func<string> factory)
//	//		=> SetErrorCode(t => factory());

//	//	public virtual RuleBuilder<T> SetErrorCode(Func<T, string> factory)
//	//	{
//	//		if (_failureFactory == null)
//	//			throw new Exception("Сначала нужно определить валидатор");

//	//		var failureFactory = _failureFactory;
//	//		_failureFactory = (t) =>
//	//		{
//	//			var failure = failureFactory(t);
//	//			failure.ErrorCode = factory(t.Instance);
//	//			return failure;
//	//		};

//	//		return this;
//	//	}

//	//	public virtual RuleBuilder<T> SetErrorMessage(string message)
//	//		=> SetErrorMessage(() => message);

//	//	public virtual RuleBuilder<T> SetErrorMessage(Func<string> factory)
//	//		=> SetErrorMessage(t => factory());

//	//	public virtual RuleBuilder<T> SetErrorMessage(Func<T, string> factory)
//	//	{
//	//		if (_failureFactory == null)
//	//			throw new Exception("Сначала нужно определить валидатор");

//	//		var failureFactory = _failureFactory;
//	//		_failureFactory = (t) =>
//	//		{
//	//			var failure = failureFactory(t);
//	//			failure.ErrorMessage = factory(t.Instance);
//	//			return failure;
//	//		};

//	//		return this;
//	//	}

//	//	public virtual RuleBuilder<T> GetAndSetPropertyNames<TProperty>(params Expression<Func<T, TProperty>>[] expressions)
//	//		=> SetPropertyNames(expressions.Select(x => x.GetMember().Name).ToArray());

//	//	public virtual RuleBuilder<T> GetAndSetPropertyName<TProperty>(Expression<Func<T, TProperty>> expression)
//	//		=> SetPropertyName(expression.GetMember().Name);
//	//	public virtual RuleBuilder<T> SetPropertyNames(params string[] propertyNames)
//	//		=> SetPropertyName('(' + string.Join(",", propertyNames) + ')');

//	//	public virtual RuleBuilder<T> SetPropertyName(string propertyName)
//	//		=> SetPropertyName(() => propertyName);

//	//	public virtual RuleBuilder<T> SetPropertyName(Func<string> factory)
//	//		=> SetPropertyName(t => factory());

//	//	public virtual RuleBuilder<T> SetPropertyName(Func<T, string> factory)
//	//	{
//	//		if (_failureFactory == null)
//	//			throw new Exception("Сначала нужно определить валидатор");

//	//		var failureFactory = _failureFactory;
//	//		_failureFactory = (t) =>
//	//		{
//	//			var failure = failureFactory(t);
//	//			failure.PropertyName = factory(t.Instance);
//	//			return failure;
//	//		};

//	//		return this;
//	//	}

//	//	public virtual RuleBuilder<T> SetAttemptedObjectIdentifier(string identifier)
//	//		=> SetAttemptedObjectIdentifier(() => identifier);

//	//	public virtual RuleBuilder<T> SetAttemptedObjectIdentifier(Func<string> factory)
//	//		=> SetAttemptedObjectIdentifier(t => factory());

//	//	public virtual RuleBuilder<T> SetAttemptedObjectIdentifier<TId>(Func<T, TId> factory)
//	//		=> SetAttemptedObjectIdentifier(t => factory(t)!.ToString()!);

//	//	public virtual RuleBuilder<T> SetAttemptedObjectIdentifier(Func<T, string> factory)
//	//	{
//	//		if (_failureFactory == null)
//	//			throw new Exception("Сначала нужно определить валидатор");

//	//		var failureFactory = _failureFactory;
//	//		_failureFactory = (t) =>
//	//		{
//	//			var failure = failureFactory(t);
//	//			failure.AttemptedObjectIdentifier = factory(t);
//	//			return failure;
//	//		};

//	//		return this;
//	//	}

//	//	public virtual RuleBuilder<T> SetAdditionalInfo(string info)
//	//		=> SetAdditionalInfo(() => info);

//	//	public virtual RuleBuilder<T> SetAdditionalInfo(Func<string> factory)
//	//		=> SetAdditionalInfo(t => factory());

//	//	public virtual RuleBuilder<T> SetAdditionalInfo(Func<T, string> factory)
//	//	{
//	//		if (_failureFactory == null)
//	//			throw new Exception("Сначала нужно определить валидатор");

//	//		var failureFactory = _failureFactory;
//	//		_failureFactory = (t) =>
//	//		{
//	//			var failure = failureFactory(t);
//	//			failure.AdditionalInfo = factory(t.Instance);
//	//			return failure;
//	//		};

//	//		return this;
//	//	}

//	//	public virtual CustomRule<T> Apply()
//	//	{
//	//		_rule.Validate = (t, results) =>
//	//		{
//	//			if (_validate(t, results))
//	//				return null;
//	//			else
//	//				return _failureFactory(t);
//	//		};
//	//		_rule.ValidateAsync = async (t, results) =>
//	//		{
//	//			if (await _validateAsync(t, results))
//	//				return null;
//	//			else
//	//				return _failureFactory(t);
//	//		};

//	//		_rules.Add(_rule);
//	//		return _rule;
//	//	}
//	//}

//	//public class CollectionRuleBuilder<TCollection, T> : RuleBuilder<T>
//	//	where TCollection : IEnumerable<T>
//	//{
//	//	public CollectionRuleBuilder(List<CustomRule<T>> rules)
//	//		: base(rules)
//	//	{
//	//	}

//	//	public virtual CollectionRuleBuilder<TCollection, T> SetValidation(Func<T, bool> validation)
//	//	{
//	//		_failureFactory = (t) => new() { Rule = _rule, };
//	//		_validate = (t, results) => validation(t);
//	//		_validateAsync = (t, results) => new(validation(t));

//	//		return this;
//	//	}


//	//	public override CustomRule<T> Apply()
//	//	{
//	//		_rule.Validate = (t, results) =>
//	//		{
//	//			var lol = t.Instances!.Select(x => _validate(new ValidationContext<T>
//	//			{
//	//				Instance = x,
//	//			}, results)
//	//			? (CustomValidationFailure?)null
//	//			: _failureFactory(new ValidationContext<T>
//	//			{
//	//				Instance = x,
//	//			}))
//	//			.Where(x => x != null)
//	//			.ToList();
//	//			return lol;

//	//			foreach (var instance in t.Instances!)
//	//			if (_validate(t, results))
//	//				return null;
//	//			else
//	//				return _failureFactory(t);
//	//		};
//	//		_rule.ValidateAsync = async (t, results) =>
//	//		{
//	//			if (await _validateAsync(t, results))
//	//				return null;
//	//			else
//	//				return _failureFactory(t);
//	//		};

//	//		_rules.Add(_rule);
//	//		return _rule;

//	//		return base.Apply();
//	//	}
//	//}

//	//public struct ValidationContext<T>
//	//{
//	//	public T Instance { get; set; }
//	//	public IEnumerable<T>? Instances { get; set; }
//	//	public Func<T, string>? IdentifierGetter { get; set; }
//	//}

//	////public static class RuleBuilder
//	////{
//	////	static public RuleBuilder<T> Create<T>() => new();
//	////}

//	//public class CustomRule<T>
//	//{
//	//	public Func<ValidationContext<T>, IReadOnlyList<CustomValidationFailure>?, ValueTask<CustomValidationFailure?>> ValidateAsync { get; set; } = default!;
//	//	public Func<ValidationContext<T>, IReadOnlyList<CustomValidationFailure>?, CustomValidationFailure?> Validate { get; set; } = default!;

//	//	public Func<ValidationContext<T>, IReadOnlyList<CustomValidationFailure>, bool>? Predicate { get; set; }
//	//}

//	//public abstract class BaseCustomValidator<TIdentifier, TEntity> : AbstractValidator<TEntity>
//	//{
//	//	private readonly Func<TIdentifier, string> _getIdentifier;
//	//	private readonly ProxiedValidator _proxiedValidator;
//	//	private readonly string _typeIdentifyer;

//	//	public override FluentValidation.Results.ValidationResult Validate(ValidationContext<TEntity> context)
//	//	{
//	//		//context.RootContextData.
//	//		return base.Validate(context);
//	//	}

//	//	public BaseCustomValidator(Func<TIdentifier, string> getIdentifier, ProxiedValidator proxiedValidator)
//	//		: base()
//	//	{
//	//		_getIdentifier = getIdentifier;
//	//		_proxiedValidator = proxiedValidator;
//	//		_typeIdentifyer = $"{Guid.NewGuid()}-{this.GetType().FullName}";
//	//	}

//	//	public BaseCustomValidator(ProxiedValidator proxiedValidator)
//	//		: this(x => x!.ToString()!, proxiedValidator)
//	//	{
//	//	}

//	//	public BaseCustomValidator(IServiceProvider services)
//	//		: this(services.GetRequiredService<ProxiedValidator>())
//	//	{
//	//	}

//	//	public class ProxiedValidator : AbstractValidator<IdentifiedEntity> { }

//	//	public struct IdentifiedEntity
//	//	{
//	//		public Func<TIdentifier, string> GetIdentifier { get; set; }
//	//		public TEntity Entity { get; set; }
//	//	}
//	//}

//	//public abstract class BaseCustomValidator<TEntity> : BaseCustomValidator<TEntity, TEntity>
//	//{
//	//	public BaseCustomValidator(Func<TEntity, string> getIdentifier, ProxiedValidator proxiedValidator)
//	//		: base(getIdentifier, proxiedValidator)
//	//	{
//	//	}

//	//	public BaseCustomValidator(ProxiedValidator proxiedValidator)
//	//		: this(x => x!.ToString()!, proxiedValidator)
//	//	{
//	//	}

//	//	public BaseCustomValidator(IServiceProvider services)
//	//		: this(services.GetRequiredService<ProxiedValidator>())
//	//	{
//	//	}
//	//}
//}
