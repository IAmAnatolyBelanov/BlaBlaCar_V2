using System.Diagnostics.CodeAnalysis;

namespace WebApi.Shared
{
	public interface IBaseOneWayMapper<TEntity, TDto>
		where TEntity : class
		where TDto : class
	{
		TDto ToDto(TEntity entity, IDictionary<object, object>? mappedObjects = null);
		void ToDto(TEntity entity, TDto dto, IDictionary<object, object>? mappedObjects = null);
		void ToDto(TEntity entity, TDto dto, Action<TDto> setter, IDictionary<object, object> mappedObjects);
		TDto ToDtoLight(TEntity entity);
		void ToDtoLight(TEntity entity, TDto dto);
		void ToDtoLight(TEntity entity, TDto dto, Action<TDto> setter);
		[return: NotNullIfNotNull(nameof(values))]
		IReadOnlyList<TDto>? ToDtoList(IEnumerable<TEntity>? values, IDictionary<object, object>? mappedObjects = null);
		[return: NotNullIfNotNull(nameof(values))]
		IReadOnlyList<TDto>? ToDtoListLight(IEnumerable<TEntity>? values);
	}

	public abstract class BaseOneWayMapper<TEntity, TDto> : IBaseOneWayMapper<TEntity, TDto> where TEntity : class
			where TDto : class
	{
		private readonly IBaseMapper<TEntity, TDto> _baseMapper;

		private class DummyBaseMapper<TDummyEntity, TDummyDto> : BaseMapper<TDummyEntity, TDummyDto>
			where TDummyEntity : class
			where TDummyDto : class
		{
			private readonly Action<TDummyEntity, TDummyDto, IDictionary<object, object>> _toDtoAction;
			private readonly Action<TDummyDto, TDummyDto> _betweenDtosAction;

			public DummyBaseMapper(
				Func<TDummyDto> dtoFactory,
				Action<TDummyEntity, TDummyDto, IDictionary<object, object>> toDtoAction,
				Action<TDummyDto, TDummyDto> betweenDtosAction)
				: base(() => throw new NotSupportedException(), dtoFactory)
			{
				_toDtoAction = toDtoAction;
				_betweenDtosAction = betweenDtosAction;
			}

			protected override void BetweenEntities(TDummyEntity from, TDummyEntity to) => throw new NotSupportedException();
			protected override void FromDtoAbstract(TDummyDto dto, TDummyEntity entity, IDictionary<object, object> mappedObjects) => throw new NotSupportedException();

			protected override void ToDtoAbstract(TDummyEntity entity, TDummyDto dto, IDictionary<object, object> mappedObjects) => _toDtoAction(entity, dto, mappedObjects);
			protected override void BetweenDtos(TDummyDto from, TDummyDto to) => _betweenDtosAction(from, to);
		}

		protected BaseOneWayMapper(Func<TDto> dtoFactory)
		{
			_baseMapper = new DummyBaseMapper<TEntity, TDto>(dtoFactory, ToDtoAbstract, BetweenDtos);
		}

		[return: NotNullIfNotNull(nameof(values))]
		public IReadOnlyList<TDto>? ToDtoListLight(IEnumerable<TEntity>? values)
			=> _baseMapper.ToDtoListLight(values);

		[return: NotNullIfNotNull(nameof(values))]
		public IReadOnlyList<TDto>? ToDtoList(IEnumerable<TEntity>? values, IDictionary<object, object>? mappedObjects = null)
			=> _baseMapper.ToDtoList(values, mappedObjects);

		public TDto ToDtoLight(TEntity entity) => _baseMapper.ToDtoLight(entity);

		public TDto ToDto(TEntity entity, IDictionary<object, object>? mappedObjects = null)
			=> _baseMapper.ToDto(entity, mappedObjects);

		public void ToDtoLight(TEntity entity, TDto dto, Action<TDto> setter)
			=> _baseMapper.ToDtoLight(entity, dto, setter);

		public void ToDto(TEntity entity, TDto dto, Action<TDto> setter, IDictionary<object, object> mappedObjects)
			=> _baseMapper.ToDto(entity, dto, setter, mappedObjects);

		public void ToDtoLight(TEntity entity, TDto dto) => _baseMapper.ToDtoLight(entity, dto);

		public void ToDto(TEntity entity, TDto dto, IDictionary<object, object>? mappedObjects = null)
			=> _baseMapper.ToDto(entity, dto, mappedObjects);

		protected abstract void ToDtoAbstract(TEntity entity, TDto dto, IDictionary<object, object> mappedObjects);
		protected abstract void BetweenDtos(TDto from, TDto to);
	}
}
