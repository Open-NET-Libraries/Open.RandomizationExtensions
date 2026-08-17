using System;
using System.Collections.Generic;

namespace Open.RandomizationExtensions;

/// <summary>
/// A <see cref="HashSet{T}"/> that fills itself lazily from an enumerator as
/// <see cref="Contains(T)"/> is called.
/// </summary>
/// <remarks>
/// IMPORTANT: <see cref="Contains(T)"/> HIDES (<see langword="new"/>) rather than
/// overrides the base method, so it only executes when called through a
/// <see cref="DeferredHashSet{T}"/>-typed reference. A call through
/// <see cref="ISet{T}"/> or <see cref="HashSet{T}"/> bypasses the lazy pump and
/// consults only what has already been materialized. Likewise <see cref="HashSet{T}.Count"/>
/// reflects only what has been pumped so far. Callers must not use Count-based
/// fast paths against an instance of this type.
/// </remarks>
sealed class DeferredHashSet<T>(IEnumerator<T> source) : HashSet<T>, IDisposable
{
	public DeferredHashSet(IEnumerable<T> source)
		: this((source ?? throw new ArgumentNullException(nameof(source))).GetEnumerator()) { }

	private readonly IEnumerator<T> Source = source ?? throw new ArgumentNullException(nameof(source));

	public new bool Contains(T item)
	{
		if (base.Contains(item))
			return true;

		// Use the same comparer as the base set so pump-time matches agree with
		// what base.Contains would later report.
		var comparer = EqualityComparer<T>.Default;
		while (Source.MoveNext())
		{
			var i = Source.Current;
			_ = Add(i);
			if (comparer.Equals(item, i))
				return true;
		}

		return false;
	}

	public void Dispose()
	{
		Source.Dispose();
		Clear();
	}
}
