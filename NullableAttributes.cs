#if NETSTANDARD2_0

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics.CodeAnalysis;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Polyfill: netstandard2.0 predates the nullable-analysis attributes.
/// The compiler recognizes this attribute by full name regardless of assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
internal sealed class MaybeNullWhenAttribute(bool returnValue) : Attribute
{
	public bool ReturnValue { get; } = returnValue;
}
#endif
