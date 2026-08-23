namespace Chatter.Rest.Hal.CodeGenerators;

/// <summary>
/// One link in the chain of types that lexically contain a <c>[HalResponse]</c> target.
/// </summary>
internal readonly struct ContainingTypeInfo : IEquatable<ContainingTypeInfo>
{
	/// <summary>The declaration keyword of the containing type, for example <c>class</c> or <c>record struct</c>.</summary>
	internal string Keyword { get; }

	/// <summary>The containing type's name including its type parameter list, for example <c>Outer&lt;T&gt;</c>.</summary>
	internal string NameWithTypeParameters { get; }

	internal ContainingTypeInfo(string keyword, string nameWithTypeParameters)
	{
		Keyword = keyword;
		NameWithTypeParameters = nameWithTypeParameters;
	}

	public bool Equals(ContainingTypeInfo other) =>
		Keyword == other.Keyword && NameWithTypeParameters == other.NameWithTypeParameters;

	public override bool Equals(object? obj) => obj is ContainingTypeInfo other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			return (Keyword.GetHashCode() * 397) ^ NameWithTypeParameters.GetHashCode();
		}
	}
}

/// <summary>
/// The equatable description of a single <c>[HalResponse]</c> target: everything the
/// <see cref="Emitter"/> needs to re-declare the type, and nothing that ties the value to a
/// particular compilation.
/// </summary>
internal readonly struct HalClassInfo : IEquatable<HalClassInfo>
{
	/// <summary>The containing namespace, or <see langword="null"/> for the global namespace.</summary>
	internal string? Namespace { get; }

	/// <summary>The containing types, outermost first; empty for a top-level type.</summary>
	internal EquatableArray<ContainingTypeInfo> ContainingTypes { get; }

	/// <summary>The target's name including its type parameter list, for example <c>Response&lt;T&gt;</c>.</summary>
	internal string NameWithTypeParameters { get; }

	/// <summary>
	/// The fully qualified metadata name, for example <c>Ns.Outer`1+Inner</c>. Arity and the
	/// containing-type chain make this unique across the compilation, so it is the dedup key.
	/// </summary>
	internal string MetadataName { get; }

	internal HalClassInfo(string? ns,
		EquatableArray<ContainingTypeInfo> containingTypes,
		string nameWithTypeParameters,
		string metadataName)
	{
		Namespace = ns;
		ContainingTypes = containingTypes;
		NameWithTypeParameters = nameWithTypeParameters;
		MetadataName = metadataName;
	}

	public bool Equals(HalClassInfo other) =>
		Namespace == other.Namespace
		&& NameWithTypeParameters == other.NameWithTypeParameters
		&& MetadataName == other.MetadataName
		&& ContainingTypes.Equals(other.ContainingTypes);

	public override bool Equals(object? obj) => obj is HalClassInfo other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = MetadataName.GetHashCode();
			hash = (hash * 397) ^ (Namespace?.GetHashCode() ?? 0);
			hash = (hash * 397) ^ NameWithTypeParameters.GetHashCode();
			hash = (hash * 397) ^ ContainingTypes.GetHashCode();
			return hash;
		}
	}
}
