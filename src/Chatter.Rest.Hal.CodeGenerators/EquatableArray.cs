using System.Collections;
using System.Collections.Immutable;

namespace Chatter.Rest.Hal.CodeGenerators;

/// <summary>
/// An <see cref="ImmutableArray{T}"/> wrapper with structural equality, so models that carry
/// collections can still act as cache keys in the incremental generator pipeline.
/// </summary>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
	where T : IEquatable<T>
{
	private readonly ImmutableArray<T> _values;

	internal EquatableArray(ImmutableArray<T> values)
	{
		_values = values;
	}

	internal static EquatableArray<T> Empty => new(ImmutableArray<T>.Empty);

	internal int Length => _values.IsDefault ? 0 : _values.Length;

	internal T this[int index] => _values[index];

	internal T[] ToArray() => _values.IsDefault ? Array.Empty<T>() : _values.ToArray();

	public bool Equals(EquatableArray<T> other)
	{
		if (_values.IsDefault || other._values.IsDefault)
		{
			return _values.IsDefault && other._values.IsDefault;
		}

		if (_values.Length != other._values.Length)
		{
			return false;
		}

		for (var i = 0; i < _values.Length; i++)
		{
			if (!_values[i].Equals(other._values[i]))
			{
				return false;
			}
		}

		return true;
	}

	public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

	public override int GetHashCode()
	{
		if (_values.IsDefault)
		{
			return 0;
		}

		unchecked
		{
			var hash = 17;
			foreach (var value in _values)
			{
				hash = (hash * 31) ^ value.GetHashCode();
			}

			return hash;
		}
	}

	public IEnumerator<T> GetEnumerator() =>
		((IEnumerable<T>)(_values.IsDefault ? ImmutableArray<T>.Empty : _values)).GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
