using System.Collections;
using System.Diagnostics.CodeAnalysis;

using WebApi.Extensions;

namespace WebApi.Shared
{
	public interface IBaseMapper<TEntity, TDto>
		where TEntity : class
		where TDto : class
	{
		TEntity FromDto(TDto dto, IDictionary<object, object>? mappedObjects = null);
		void FromDto(TDto dto, TEntity entity, IDictionary<object, object>? mappedObjects = null);
		void FromDto(TDto dto, TEntity entity, Action<TEntity> setter, IDictionary<object, object>? mappedObjects);
		TEntity FromDtoLight(TDto dto);
		void FromDtoLight(TDto dto, TEntity entity);
		void FromDtoLight(TDto dto, TEntity entity, Action<TEntity> setter);
		IReadOnlyList<TEntity>? FromDtoList(IEnumerable<TDto>? values, IDictionary<object, object>? mappedObjects = null);
		IReadOnlyList<TEntity>? FromDtoListLight(IEnumerable<TDto>? values);
		TDto ToDto(TEntity entity, IDictionary<object, object>? mappedObjects = null);
		void ToDto(TEntity entity, TDto dto, IDictionary<object, object>? mappedObjects = null);
		void ToDto(TEntity entity, TDto dto, Action<TDto> setter, IDictionary<object, object> mappedObjects);
		TDto ToDtoLight(TEntity entity);
		void ToDtoLight(TEntity entity, TDto dto);
		void ToDtoLight(TEntity entity, TDto dto, Action<TDto> setter);
		IReadOnlyList<TDto>? ToDtoList(IEnumerable<TEntity>? values, IDictionary<object, object>? mappedObjects = null);
		IReadOnlyList<TDto>? ToDtoListLight(IEnumerable<TEntity>? values);
	}

	public abstract class BaseMapper<TEntity, TDto> : IBaseMapper<TEntity, TDto>
		where TEntity : class
		where TDto : class
	{
		private class FakeDict : IDictionary<object, object>
		{
			private FakeDict()
			{
			}

			public static FakeDict Shared { get; } = new FakeDict();
			private static Dictionary<object, object> _fake = new();

			public object this[object key] { get => throw new NotImplementedException(); set { } }

			public ICollection<object> Keys => Array.Empty<object>();

			public ICollection<object> Values => Array.Empty<object>();

			public int Count => 0;

			public bool IsReadOnly => false;

			public void Add(object key, object value) { }
			public void Add(KeyValuePair<object, object> item) { }
			public void Clear() { }
			public bool Contains(KeyValuePair<object, object> item) => false;
			public bool ContainsKey(object key) => false;
			public void CopyTo(KeyValuePair<object, object>[] array, int arrayIndex) => throw new NotImplementedException();
			public IEnumerator<KeyValuePair<object, object>> GetEnumerator() => _fake.GetEnumerator();
			public bool Remove(object key) => false;
			public bool Remove(KeyValuePair<object, object> item) => false;
			public bool TryGetValue(object key, [MaybeNullWhen(false)] out object value)
			{
				value = default;
				return false;
			}

			IEnumerator IEnumerable.GetEnumerator() => _fake.GetEnumerator();
		}

		private readonly Func<TEntity> _entityFactory;
		private readonly Func<TDto> _dtoFactory;

		protected BaseMapper(Func<TEntity> entityFactory, Func<TDto> dtoFactory)
		{
			_entityFactory = entityFactory;
			_dtoFactory = dtoFactory;
		}

		public IReadOnlyList<TDto>? ToDtoListLight(IEnumerable<TEntity>? values)
			=> ToDtoList(values, FakeDict.Shared);

		public IReadOnlyList<TDto>? ToDtoList(IEnumerable<TEntity>? values, IDictionary<object, object>? mappedObjects = null)
		{
			if (values is null)
				return null;

			if (mappedObjects is null)
				mappedObjects = new Dictionary<object, object>();

			if (values is IReadOnlyList<TEntity> list)
				return ToDtoList(list, mappedObjects);

			if (values is IReadOnlyCollection<TEntity> collection)
				return ToDtoList(collection, mappedObjects);

			var result = values
				.Select(x => ToDto(x, mappedObjects))
				.ToArray();

			return result;
		}

		private IReadOnlyList<TDto> ToDtoList(IReadOnlyList<TEntity> list, IDictionary<object, object> mappedObjects)
		{
			if (list.Count == 0)
				return Array.Empty<TDto>();

			var result = new TDto[list.Count];

			for (int i = 0; i < list.Count; i++)
				result[i] = ToDto(list[i], mappedObjects);

			return result;
		}

		private IReadOnlyList<TDto> ToDtoList(IReadOnlyCollection<TEntity> list, IDictionary<object, object> mappedObjects)
		{
			if (list.Count == 0)
				return Array.Empty<TDto>();

			var result = list
				.Select(x => ToDto(x, mappedObjects))
				.ToArray(list.Count);

			return result;
		}

		public TDto ToDtoLight(TEntity entity) => ToDto(entity, FakeDict.Shared);

		public TDto ToDto(TEntity entity, IDictionary<object, object>? mappedObjects = null)
		{
			if (mappedObjects is null)
				mappedObjects = new Dictionary<object, object>();

			return ToDtoBase(entity, mappedObjects);
		}

		public void ToDtoLight(TEntity entity, TDto dto, Action<TDto> setter)
			=> ToDto(entity, dto, setter, FakeDict.Shared);

		public void ToDto(TEntity entity, TDto dto, Action<TDto> setter, IDictionary<object, object> mappedObjects)
		{
			if (entity is null)
			{
				setter(default!);
				return;
			}

			if (dto is null)
				setter(ToDto(entity, mappedObjects));
			else
				ToDto(entity, dto, mappedObjects);
		}

		protected TDto ToDtoBase(TEntity entity, IDictionary<object, object> mappedObjects)
		{
			if (mappedObjects.TryGetValue(entity!, out var mappedObject))
				return (TDto)mappedObject;

			var dto = _dtoFactory();
			ToDto(entity, dto, mappedObjects);
			return dto;
		}

		public void ToDtoLight(TEntity entity, TDto dto) => ToDto(entity, dto, FakeDict.Shared);

		public void ToDto(TEntity entity, TDto dto, IDictionary<object, object>? mappedObjects = null)
		{
			if (mappedObjects is null)
				mappedObjects = new Dictionary<object, object>();

			ToDtoBase(entity, dto, mappedObjects);
		}

		protected void ToDtoBase(TEntity entity, TDto dto, IDictionary<object, object> mappedObjects)
		{
			if (mappedObjects.TryGetValue(entity!, out var mappedObject))
			{
				if (object.Equals(dto, mappedObject))
					return;

				BetweenDtos(from: (TDto)mappedObject, to: dto);
				return;
			}

			mappedObjects.TryAdd(entity, dto);
			mappedObjects.TryAdd(dto, entity);

			ToDtoAbstract(entity, dto, mappedObjects);
		}

		public IReadOnlyList<TEntity>? FromDtoListLight(IEnumerable<TDto>? values)
			=> FromDtoList(values, FakeDict.Shared);

		public IReadOnlyList<TEntity>? FromDtoList(IEnumerable<TDto>? values, IDictionary<object, object>? mappedObjects = null)
		{
			if (values is null)
				return null;

			if (mappedObjects is null)
				mappedObjects = new Dictionary<object, object>();

			if (values is IReadOnlyList<TDto> list)
				return FromDtoList(list, mappedObjects);

			if (values is IReadOnlyCollection<TDto> collection)
				return FromDtoList(collection, mappedObjects);

			var result = values
				.Select(x => FromDto(x, mappedObjects))
				.ToArray();

			return result;
		}

		private IReadOnlyList<TEntity> FromDtoList(IReadOnlyList<TDto> list, IDictionary<object, object> mappedObjects)
		{
			if (list.Count == 0)
				return Array.Empty<TEntity>();

			var result = new TEntity[list.Count];

			for (int i = 0; i < list.Count; i++)
				result[i] = FromDto(list[i], mappedObjects);

			return result;
		}

		private IReadOnlyList<TEntity> FromDtoList(IReadOnlyCollection<TDto> list, IDictionary<object, object> mappedObjects)
		{
			if (list.Count == 0)
				return Array.Empty<TEntity>();

			var result = list
				.Select(x => FromDto(x, mappedObjects))
				.ToArray(list.Count);

			return result;
		}

		public TEntity FromDtoLight(TDto dto) => FromDto(dto, FakeDict.Shared);

		public TEntity FromDto(TDto dto, IDictionary<object, object>? mappedObjects = null)
		{
			if (mappedObjects is null)
				mappedObjects = new Dictionary<object, object>();

			return FromDtoBase(dto, mappedObjects);
		}

		public void FromDtoLight(TDto dto, TEntity entity, Action<TEntity> setter)
			=> FromDto(dto, entity, setter, FakeDict.Shared);

		public void FromDto(TDto dto, TEntity entity, Action<TEntity> setter, IDictionary<object, object>? mappedObjects)
		{
			if (dto is null)
			{
				setter(default!);
				return;
			}

			if (entity is null)
				setter(FromDto(dto, mappedObjects));
			else
				FromDto(dto, entity, mappedObjects);
		}

		protected TEntity FromDtoBase(TDto dto, IDictionary<object, object> mappedObjects)
		{
			if (mappedObjects.TryGetValue(dto, out var mappedObject))
				return (TEntity)mappedObject;

			var entity = _entityFactory();
			FromDto(dto, entity, mappedObjects);
			return entity;
		}

		public void FromDtoLight(TDto dto, TEntity entity) => FromDto(dto, entity, FakeDict.Shared);

		public void FromDto(TDto dto, TEntity entity, IDictionary<object, object>? mappedObjects = null)
		{
			if (mappedObjects is null)
				mappedObjects = new Dictionary<object, object>();

			FromDtoBase(dto, entity, mappedObjects);
		}

		protected void FromDtoBase(TDto dto, TEntity entity, IDictionary<object, object> mappedObjects)
		{
			if (mappedObjects.TryGetValue(dto, out var mappedObject))
			{
				if (object.Equals(entity, mappedObject))
					return;

				BetweenEntities(from: (TEntity)mappedObject, to: entity);
				return;
			}

			mappedObjects.TryAdd(dto, entity);
			mappedObjects.TryAdd(entity, dto);

			FromDtoAbstract(dto, entity, mappedObjects);
		}

		protected abstract void ToDtoAbstract(TEntity entity, TDto dto, IDictionary<object, object> mappedObjects);
		protected abstract void FromDtoAbstract(TDto dto, TEntity entity, IDictionary<object, object> mappedObjects);

		protected abstract void BetweenDtos(TDto from, TDto to);
		protected abstract void BetweenEntities(TEntity from, TEntity to);
	}

}
