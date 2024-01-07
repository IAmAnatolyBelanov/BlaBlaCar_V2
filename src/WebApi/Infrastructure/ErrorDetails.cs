using Riok.Mapperly.Abstractions;

namespace WebApi.Infrastructure
{
	public class ErrorDetails
	{
		public string Code { get; set; } = default!;
		public string Message { get; set; } = default!;
		public string? AdditionalInfo { get; set; }
	}

	public interface IErrorDetailsMapper : IBaseMapper<ValidationFailure, ErrorDetails>
	{
	}

	[Mapper]
	public partial class ErrorDetailsMapper : BaseMapper<ValidationFailure, ErrorDetails>, IErrorDetailsMapper
	{
		public ErrorDetailsMapper()
			: base(() => throw new NotSupportedException(), () => new())
		{
		}

		protected override void BetweenDtos(ErrorDetails from, ErrorDetails to)
			=> throw new NotSupportedException();
		protected override void BetweenEntities(ValidationFailure from, ValidationFailure to)
			=> throw new NotSupportedException();
		protected override void FromDtoAbstract(ErrorDetails dto, ValidationFailure entity, IDictionary<object, object> mappedObjects)
			=> throw new NotSupportedException();
		protected override void ToDtoAbstract(ValidationFailure entity, ErrorDetails dto, IDictionary<object, object> mappedObjects)
		{
			dto.Code = entity.ErrorCode;
			dto.Message = entity.ErrorMessage;
			dto.AdditionalInfo = string.IsNullOrWhiteSpace(entity.PropertyName)
				? null
				: $"{nameof(ValidationFailure.PropertyName)} - {entity.PropertyName}";
		}
	}
}
