/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.RandomizationExtensions/blob/master/LICENSE
 */

#if NET10_0_OR_GREATER

using System;
using System.Diagnostics.CodeAnalysis;
using System.Buffers;
using System.Collections.Generic;

namespace Open.RandomizationExtensions;

public static partial class Randomizer
{
	// C# 13 params-span overloads for the exclusion family. Callers writing
	// source.RandomSelectOneExcept('a', 'b') no longer allocate a params array per
	// call, and the exclusions are checked by direct linear scan -- for the small
	// exclusion counts these methods exist for, that beats building a set.
	// Overload resolution prefers these over the params T[] forms for literal
	// arguments; explicitly-typed arrays still bind the array overloads.

	private static bool SpanContains<T>(ReadOnlySpan<T> values, T value)
	{
		var comparer = EqualityComparer<T>.Default;
		foreach (var v in values)
		{
			if (comparer.Equals(v, value))
				return true;
		}

		return false;
	}

	/// <inheritdoc cref="RandomSelectIndexExcept{T}(in ReadOnlySpan{T}, Random?, T, T[])"/>
	public static int RandomSelectIndexExcept<T>(
		this in ReadOnlySpan<T> source, Random? random, T excluding, params ReadOnlySpan<T> others)
	{
		if (source.Length == 0)
			return -1;

		var pool = ArrayPool<int>.Shared;
		var indexes = pool.Rent(source.Length);
		try
		{
			var comparer = EqualityComparer<T>.Default;
			var indexCount = 0;
			for (var i = 0; i < source.Length; ++i)
			{
				var value = source[i];
				if (!comparer.Equals(excluding, value) && !SpanContains(others, value))
					indexes[indexCount++] = i;
			}

			return indexCount == 0 ? -1 : indexes[(random ?? Default).Next(indexCount)];
		}
		finally
		{
			pool.Return(indexes);
		}
	}

	/// <inheritdoc cref="RandomSelectIndexExcept{T}(in ReadOnlySpan{T}, T, T[])"/>
	public static int RandomSelectIndexExcept<T>(
		this in ReadOnlySpan<T> source, T excluding, params ReadOnlySpan<T> others)
		=> RandomSelectIndexExcept(in source, null, excluding, others);

	/// <inheritdoc cref="TryRandomSelectOneExcept{T}(in ReadOnlySpan{T}, out T, Random?, T, T[])"/>
	public static bool TryRandomSelectOneExcept<T>(
		this in ReadOnlySpan<T> source, [MaybeNullWhen(false)] out T value, Random? random, T excluding, params ReadOnlySpan<T> others)
	{
		var index = RandomSelectIndexExcept(in source, random, excluding, others);
		if (index == -1)
		{
			value = default;
			return false;
		}

		value = source[index];
		return true;
	}

	/// <inheritdoc cref="TryRandomSelectOneExcept{T}(in ReadOnlySpan{T}, out T, T, T[])"/>
	public static bool TryRandomSelectOneExcept<T>(
		this in ReadOnlySpan<T> source, [MaybeNullWhen(false)] out T value, T excluding, params ReadOnlySpan<T> others)
		=> TryRandomSelectOneExcept(in source, out value, null, excluding, others);

	/// <inheritdoc cref="RandomSelectOneExcept{T}(in ReadOnlySpan{T}, Random?, T, T[])"/>
	public static T RandomSelectOneExcept<T>(
		this in ReadOnlySpan<T> source, Random? random, T excluding, params ReadOnlySpan<T> others)
		=> source.Length == 0
			? throw new InvalidOperationException("Source collection is empty.")
			: TryRandomSelectOneExcept(in source, out var value, random, excluding, others)
			? value
			: throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");

	/// <inheritdoc cref="RandomSelectOneExcept{T}(in ReadOnlySpan{T}, T, T[])"/>
	public static T RandomSelectOneExcept<T>(
		this in ReadOnlySpan<T> source, T excluding, params ReadOnlySpan<T> others)
		=> RandomSelectOneExcept(in source, null, excluding, others);

	/// <inheritdoc cref="TryRandomSelectOneExcept{T}(IReadOnlyCollection{T}, out T, Random?, T, T[])"/>
	public static bool TryRandomSelectOneExcept<T>(
		this IReadOnlyCollection<T> source, [MaybeNullWhen(false)] out T value, Random? random, T excluding, params ReadOnlySpan<T> others)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));

		if (source.Count == 0)
		{
			value = default;
			return false;
		}

		var pool = ArrayPool<int>.Shared;
		var indexes = pool.Rent(source.Count);
		try
		{
			var comparer = EqualityComparer<T>.Default;
			var i = -1;
			var indexCount = 0;
			foreach (var v in source)
			{
				++i;
				if (!comparer.Equals(excluding, v) && !SpanContains(others, v))
					indexes[indexCount++] = i;
			}

			if (indexCount == 0)
			{
				value = default;
				return false;
			}

			value = GetElementAt(source, indexes[(random ?? Default).Next(indexCount)]);
			return true;
		}
		finally
		{
			pool.Return(indexes);
		}
	}

	/// <inheritdoc cref="TryRandomSelectOneExcept{T}(IReadOnlyCollection{T}, out T, T, T[])"/>
	public static bool TryRandomSelectOneExcept<T>(
		this IReadOnlyCollection<T> source, [MaybeNullWhen(false)] out T value, T excluding, params ReadOnlySpan<T> others)
		=> TryRandomSelectOneExcept(source, out value, null, excluding, others);

	/// <inheritdoc cref="RandomSelectOneExcept{T}(IReadOnlyCollection{T}, Random?, T, T[])"/>
	public static T RandomSelectOneExcept<T>(
		this IReadOnlyCollection<T> source, Random? random, T excluding, params ReadOnlySpan<T> others)
		=> source is null ? throw new ArgumentNullException(nameof(source))
			: source.Count == 0
			? throw new InvalidOperationException("Source collection is empty.")
			: TryRandomSelectOneExcept(source, out var value, random, excluding, others)
			? value
			: throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");

	/// <inheritdoc cref="RandomSelectOneExcept{T}(IReadOnlyCollection{T}, T, T[])"/>
	public static T RandomSelectOneExcept<T>(
		this IReadOnlyCollection<T> source, T excluding, params ReadOnlySpan<T> others)
		=> RandomSelectOneExcept(source, null, excluding, others);

	/// <inheritdoc cref="NextExcluding(Random, int, int, int[])"/>
	public static int NextExcluding(this Random source,
		int range, int excluding, params ReadOnlySpan<int> others)
	{
		if (source is null) throw new ArgumentNullException(nameof(source));

		if (range <= 0)
			throw new ArgumentOutOfRangeException(nameof(range), range, "Must be a number greater than zero.");

		if (others.Length == 0)
		{
			// Inline the single-exclusion skip-shift rather than re-dispatching:
			// a call with no 'others' (e.g. NextExcluding(range, x)) binds THIS overload
			// under C# 13's params-span preference, so delegating to "the two-argument
			// form" would recurse right back here forever.
			if (excluding >= range || excluding < 0)
				return source.Next(range);

			if (excluding == 0 && range == 1)
				throw new ArgumentException("No value is available with a range of 1 and exclusion of 0.", nameof(range));

			var single = source.Next(range - 1);
			return single < excluding ? single : single + 1;
		}

		var pool = ArrayPool<int>.Shared;
		var candidates = pool.Rent(range);
		try
		{
			var count = 0;
			for (var i = 0; i < range; ++i)
			{
				if (i != excluding && !SpanContains<int>(others, i))
					candidates[count++] = i;
			}

			return count == 0
				? throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.")
				: candidates[source.Next(count)];
		}
		finally
		{
			pool.Return(candidates);
		}
	}
}

#endif
