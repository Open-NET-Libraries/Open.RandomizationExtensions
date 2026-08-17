#if NETSTANDARD2_0
using System;

namespace System.Diagnostics.CodeAnalysis
{
	/// <summary>
	/// Polyfill: netstandard2.0 predates the nullable-analysis attributes.
	/// The compiler recognizes this attribute by full name regardless of assembly.
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter)]
	internal sealed class MaybeNullWhenAttribute : Attribute
	{
		public MaybeNullWhenAttribute(bool returnValue) => ReturnValue = returnValue;
		public bool ReturnValue { get; }
	}
}
#endif
