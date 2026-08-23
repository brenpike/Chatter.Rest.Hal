using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Chatter.Rest.Hal.CodeGenerators;

/// <summary>
/// An equatable stand-in for <see cref="Location"/>. A <see cref="Location"/> holds a reference to
/// its <see cref="SyntaxTree"/>, which gets a new identity on every edit; storing the position data
/// instead keeps values that flow through the incremental pipeline comparable.
/// </summary>
internal readonly struct LocationInfo : IEquatable<LocationInfo>
{
	private readonly string _filePath;
	private readonly TextSpan _textSpan;
	private readonly LinePositionSpan _lineSpan;

	private LocationInfo(string filePath, TextSpan textSpan, LinePositionSpan lineSpan)
	{
		_filePath = filePath;
		_textSpan = textSpan;
		_lineSpan = lineSpan;
	}

	internal static LocationInfo? CreateFrom(Location? location) =>
		location?.SourceTree is null
			? null
			: new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);

	internal Location ToLocation() => Location.Create(_filePath, _textSpan, _lineSpan);

	public bool Equals(LocationInfo other) =>
		_filePath == other._filePath && _textSpan.Equals(other._textSpan) && _lineSpan.Equals(other._lineSpan);

	public override bool Equals(object? obj) => obj is LocationInfo other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = _filePath?.GetHashCode() ?? 0;
			hash = (hash * 397) ^ _textSpan.GetHashCode();
			hash = (hash * 397) ^ _lineSpan.GetHashCode();
			return hash;
		}
	}
}

/// <summary>
/// An equatable description of a diagnostic. The <see cref="Diagnostic"/> itself is materialized
/// only when the generator reports it.
/// </summary>
internal readonly struct DiagnosticInfo : IEquatable<DiagnosticInfo>
{
	private readonly DiagnosticDescriptor _descriptor;
	private readonly LocationInfo? _location;
	private readonly EquatableArray<string> _messageArgs;

	private DiagnosticInfo(DiagnosticDescriptor descriptor, LocationInfo? location, ImmutableArray<string> messageArgs)
	{
		_descriptor = descriptor;
		_location = location;
		_messageArgs = new EquatableArray<string>(messageArgs);
	}

	internal static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, params string[] messageArgs) =>
		new(descriptor,
			LocationInfo.CreateFrom(location),
			messageArgs is null ? ImmutableArray<string>.Empty : messageArgs.ToImmutableArray());

	internal string Id => _descriptor.Id;

	internal Diagnostic ToDiagnostic() =>
		Diagnostic.Create(_descriptor, _location?.ToLocation(), _messageArgs.ToArray());

	public bool Equals(DiagnosticInfo other) =>
		ReferenceEquals(_descriptor, other._descriptor)
		&& Nullable.Equals(_location, other._location)
		&& _messageArgs.Equals(other._messageArgs);

	public override bool Equals(object? obj) => obj is DiagnosticInfo other && Equals(other);

	public override int GetHashCode()
	{
		unchecked
		{
			var hash = _descriptor?.Id.GetHashCode() ?? 0;
			hash = (hash * 397) ^ (_location?.GetHashCode() ?? 0);
			hash = (hash * 397) ^ _messageArgs.GetHashCode();
			return hash;
		}
	}
}
