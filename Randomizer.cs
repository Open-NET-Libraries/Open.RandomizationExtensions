/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Genetic-Algorithm-Platform/blob/master/LICENSE.md
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Open.RandomizationExtensions;

public static class Randomizer
{
	static readonly Lazy<Random> R = new(() => new Random());

	/// <summary>
	/// The Random object used by the extensions.
	/// </summary>
	public static Random Random => R.Value;

	/// <summary>
	/// Attempts to select a LinkedListNode at random and remove it.
	/// </summary>
	/// <typeparam name="T">The generic type of the linked list.</typeparam>
	/// <param name="source">The source linked list.</param>
	/// <param name="value">The value retrieved.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <returns>True if successfully retrieved and item and removed the node.</returns>
	public static bool TryRandomPluck<T>(this LinkedList<T> source, out T value, Random? random = null)
	{
		if (source.Count == 0)
		{
			value = default!;
			return false;
		}

		var r = (random ?? R.Value).Next(source.Count);
		var node = source.First;
		for (var i = 0; i <= r; i++)
			node = node.Next;
		value = node.Value;
		source.Remove(node);
		return true;
	}

	/// <summary>
	/// Selects a LinkedListNode at random, removes it, and returns its value.
	/// </summary>
	/// <typeparam name="T">The generic type of the linked list.</typeparam>
	/// <param name="source">The source linked list.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <returns>The value retrieved.</returns>
	public static T RandomPluck<T>(this LinkedList<T> source, Random? random = null)
		=> source.TryRandomPluck(out var value, random) ? value
		: throw new InvalidOperationException("Source collection is empty.");

	/// <summary>
	/// Attempts to select an index at random and remove it.
	/// </summary>
	/// <typeparam name="T">The generic type of the list.</typeparam>
	/// <param name="source">The source list.</param>
	/// <param name="value">The value removed.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <returns>True if successfully removed.</returns>
	public static bool TryRandomPluck<T>(this List<T> source, out T value, Random? random = null)
	{
		if (source.Count == 0)
		{
			value = default!;
			return false;
		}

		var r = (random ?? R.Value).Next(source.Count);
		value = source[r];
		source.RemoveAt(r);
		return true;
	}

	/// <summary>
	/// Selects an index at random, removes it, and returns its value.
	/// </summary>
	/// <typeparam name="T">The generic type of the list.</typeparam>
	/// <param name="source">The source list.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <returns>The value retrieved.</returns>
	public static T RandomPluck<T>(this List<T> source, Random? random = null)
		=> source.TryRandomPluck(out var value, random) ? value
		: throw new InvalidOperationException("Source collection is empty.");

	/// <summary>
	/// Randomly selects a reference from the source.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <returns>The reference selected.</returns>
	public static ref readonly T RandomSelectOne<T>(
		this in ReadOnlySpan<T> source, Random? random = null)
	{
		if (source.Length == 0)
			throw new InvalidOperationException("Source collection is empty.");

		return ref source[(random ?? R.Value).Next(source.Length)];
	}

	/// <summary>
	/// Randomly selects a reference from the source.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <returns>The reference selected.</returns>
	public static ref T RandomSelectOne<T>(
		this in Span<T> source, Random? random = null)
	{
		if (source.Length == 0)
			throw new InvalidOperationException("Source collection is empty.");

		return ref source[(random ?? R.Value).Next(source.Length)];
	}

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="exclusion">The optional values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndex<T>(this in ReadOnlySpan<T> source, Random? random = null, IEnumerable<T>? exclusion = null)
	{
		if (source.Length == 0)
			return -1;

		DeferredHashSet<T>? setCreated = null;
		try
		{
			var exclusionSet = exclusion == null || exclusion is ICollection<T> c && c.Count == 0 ? default
				: exclusion as ISet<T> ?? (setCreated = new DeferredHashSet<T>(exclusion));

			if (exclusionSet == null || exclusionSet.Count == 0)
				return (random ?? R.Value).Next(source.Length);

			if (exclusionSet.Count == 1)
				return RandomSelectIndexExcept(in source, random, exclusionSet.Single());

			var count = source.Length;
			var pool = ArrayPool<int>.Shared;
			var indexes = pool.Rent(count);
			try
			{
				var indexCount = 0;
				for (var i = 0; i < count; ++i)
				{
					if (!exclusionSet.Contains(source[i]))
						indexes[indexCount++] = i;
				}

				return indexCount == 0 ? -1
					: indexes[(random ?? R.Value).Next(indexCount)];
			}
			finally
			{
				pool.Return(indexes);
			}
		}
		finally
		{
			setCreated?.Dispose();
		}
	}

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="exclusion">The values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndex<T>(this in ReadOnlySpan<T> source, IEnumerable<T> exclusion)
		=> RandomSelectIndex(in source, default, exclusion);

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The optional values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndexExcept<T>(this in ReadOnlySpan<T> source, Random? random, T excluding, params T[] others)
	{
		if (source.Length == 0)
			return -1;

		if (others.Length != 0)
			return RandomSelectIndex(in source, random, others.Prepend(excluding));

		var pool = ArrayPool<int>.Shared;
		var indexes = pool.Rent(others.Length);
		try
		{
			var i = -1;
			var indexCount = 0;
			foreach (var value in source)
			{
				++i;
				bool equals = excluding is null ? value is null : excluding.Equals(value);
				if (!equals)
					indexes[indexCount++] = i;
			}

			return indexCount == 0 ? -1 : indexes[R.Value.Next(indexCount)];
		}
		finally
		{
			pool.Return(indexes);
		}
	}

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="exclusion">A value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndexExcept<T>(this in ReadOnlySpan<T> source, T excluding, params T[] others)
		=> RandomSelectIndexExcept(in source, default, excluding, others);

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="exclusion">The optional values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndex<T>(this in Span<T> source, Random? random = null, IEnumerable<T>? exclusion = null)
		=> RandomSelectIndex((ReadOnlySpan<T>)source, random, exclusion);

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="exclusion">The values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndex<T>(this in Span<T> source, IEnumerable<T> exclusion)
		=> RandomSelectIndex((ReadOnlySpan<T>)source, default, exclusion);

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="excluding">A value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndexExcept<T>(this in Span<T> source, Random random, T excluding, params T[] others)
		=> RandomSelectIndexExcept((ReadOnlySpan<T>)source, random, excluding, others);

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="excluding">A value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndexExcept<T>(this in Span<T> source, T excluding, params T[] others)
		=> RandomSelectIndexExcept((ReadOnlySpan<T>)source, default, excluding, others);

	static int RandomSelectIndex<T>(Random? random, int count, IEnumerable<T> source, IEnumerable<T>? exclusion)
	{
		if (count == 0)
			return -1;

		DeferredHashSet<T>? setCreated = null;
		try
		{
			var exclusionSet = exclusion == null ? default
				: exclusion as ISet<T> ?? (setCreated = new DeferredHashSet<T>(exclusion));

			if (exclusionSet == null || exclusionSet.Count == 0)
				return (random ?? R.Value).Next(count);

			if (exclusionSet.Count == 1)
				return RandomSelectIndexExcept(random, count, source, exclusionSet.Single());

			var pool = ArrayPool<int>.Shared;
			var indexes = pool.Rent(count);
			try
			{
				var i = -1;
				var indexCount = 0;
				foreach (var value in source)
				{
					++i;
					if (!exclusionSet.Contains(value))
						indexes[indexCount++] = i;
				}

				return indexCount == 0 ? -1 : indexes[(random ?? R.Value).Next(indexCount)];
			}
			finally
			{
				pool.Return(indexes);
			}
		}
		finally
		{
			setCreated?.Dispose();
		}
	}

	static int RandomSelectIndexExcept<T>(Random? random, int count, IEnumerable<T> source, T excluding, params T[] others)
	{
		if (count == 0)
			return -1;

		if (others.Length != 0)
			return RandomSelectIndex(random, count, source, others.Prepend(excluding));

		var pool = ArrayPool<int>.Shared;
		var indexes = pool.Rent(count);
		try
		{
			var i = -1;
			var indexCount = 0;
			foreach (var value in source)
			{
				++i;
				bool equals = excluding is null ? value is null : excluding.Equals(value);
				if (!equals)
					indexes[indexCount++] = i;
			}

			return indexCount == 0 ? -1 : indexes[R.Value.Next(indexCount)];
		}
		finally
		{
			pool.Return(indexes);
		}
	}

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="exclusion">The optional values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndex<T>(this IReadOnlyCollection<T> source, Random? random = null, IEnumerable<T>? exclusion = null)
		=> RandomSelectIndex(random, source.Count, source, exclusion);

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="exclusion">A value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndexExcept<T>(this IReadOnlyCollection<T> source, Random random, T exclusion, params T[] others)
		=> RandomSelectIndexExcept(random, source.Count, source, exclusion, others);

	/// <summary>
	/// Randomly selects an index from the source.
	/// Will not return indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="exclusion">A value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The index selected.</returns>
	public static int RandomSelectIndexExcept<T>(this IReadOnlyCollection<T> source, T exclusion, params T[] others)
		=> RandomSelectIndexExcept(default, source.Count, source, exclusion, others);

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="exclusion">The values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOne<T>(
		this in ReadOnlySpan<T> source,
		out T value,
		Random? random = null,
		IEnumerable<T>? exclusion = null)
	{
		var index = RandomSelectIndex(in source, random, exclusion);
		if (index == -1)
		{
			value = default!;
			return false;
		}

		value = source[index];
		return true;
	}

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="exclusion">The values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOne<T>(
		this in ReadOnlySpan<T> source,
		out T value,
		IEnumerable<T> exclusion)
		=> TryRandomSelectOne(in source, out value, null, exclusion);

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="exclusion">The optional values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOne<T>(
		this in Span<T> source,
		out T value,
		Random? random = null,
		IEnumerable<T>? exclusion = null)
		=> TryRandomSelectOne((ReadOnlySpan<T>)source, out value, random, exclusion);

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="exclusion">The values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOne<T>(
		this in Span<T> source,
		out T value,
		IEnumerable<T> exclusion)
		=> TryRandomSelectOne((ReadOnlySpan<T>)source, out value, null, exclusion);

	static T GetElementAt<T>(IEnumerable<T> source, int index)
		=> source switch
		{
			IReadOnlyList<T> readOnlyList => readOnlyList[index],
			IList<T> list => list[index],
			_ => source.ElementAt(index)
		};

	/// <summary>
	/// Selects an index at random from the source and returns the value from it.
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="exclusion">The optional values to exclude from selection.</param>
	/// <returns>The value selected.</returns>
	public static T RandomSelectOne<T>(
		this IReadOnlyCollection<T> source,
		Random? random = null,
		IEnumerable<T>? exclusion = null)
	{
		if (source.Count == 0)
			throw new InvalidOperationException("Source collection is empty.");

		var index = RandomSelectIndex(random, source.Count, source, exclusion);
		return index == -1
			? throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.")
			: GetElementAt(source, index);
	}

	/// <summary>
	/// Selects an index at random from the source and returns the value from it.
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="exclusion">The values to exclude from selection.</param>
	/// <returns>The value selected.</returns>
	public static T RandomSelectOne<T>(
		this IReadOnlyCollection<T> source,
		IEnumerable<T> exclusion)
		=> RandomSelectOne(source, default, exclusion);

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="exclusion">The optional values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOne<T>(
		this IReadOnlyCollection<T> source,
		out T value,
		Random? random = null,
		IEnumerable<T>? exclusion = null)
	{
		var index = RandomSelectIndex(random, source.Count, source, exclusion);
		if (index == -1)
		{
			value = default!;
			return false;
		}

		value = GetElementAt(source, index);

		return true;
	}

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="exclusion">The values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOne<T>(
		this IReadOnlyCollection<T> source,
		out T value,
		IEnumerable<T> exclusion)
		=> TryRandomSelectOne(source, out value, default, exclusion);

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOneExcept<T>(
		this in ReadOnlySpan<T> source,
		out T value,
		Random? random,
		T excluding, params T[] others)
	{
		var index = RandomSelectIndexExcept(in source, random, excluding, others);
		if (index == -1)
		{
			value = default!;
			return false;
		}

		value = source[index];
		return true;
	}

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOneExcept<T>(
		this in ReadOnlySpan<T> source,
		out T value,
		T excluding, params T[] others)
		=> TryRandomSelectOneExcept(in source, out value, default, excluding, others);

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="excluding">The optional values to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOneExcept<T>(
		this in Span<T> source,
		out T value,
		Random? random,
		T excluding, params T[] others)
		=> TryRandomSelectOneExcept((ReadOnlySpan<T>)source, out value, random, excluding, others);

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOneExcept<T>(
		this IReadOnlyCollection<T> source,
		out T value,
		Random? random,
		T excluding, params T[] others)
	{
		var index = RandomSelectIndexExcept(random, source.Count, source, excluding, others);
		if (index == -1)
		{
			value = default!;
			return false;
		}

		value = GetElementAt(source, index);

		return true;
	}

	/// <summary>
	/// Attempts to select an index at random from the source and returns the value from it..
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="value">The value selected.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>True if a valid value was selected.</returns>
	public static bool TryRandomSelectOneExcept<T>(
		this IReadOnlyCollection<T> source,
		out T value,
		T excluding, params T[] others)
		=> TryRandomSelectOneExcept(source, out value, default, excluding, others);

	/// <summary>
	/// Selects an index at random from the source and returns the value from it.
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The value selected.</returns>
	public static T RandomSelectOneExcept<T>(
		this in ReadOnlySpan<T> source,
		Random? random,
		T excluding, params T[] others)
		=> source.Length == 0
			? throw new InvalidOperationException("Source collection is empty.")
			: TryRandomSelectOneExcept(in source, out T value, random, excluding, others)
			? value
			: throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");

	/// <summary>
	/// Selects an index at random from the source and returns the value from it.
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The value selected.</returns>
	public static T RandomSelectOneExcept<T>(
		this in ReadOnlySpan<T> source,
		T excluding, params T[] others)
		=> RandomSelectOneExcept(in source, default, excluding, others);

	/// <summary>
	/// Selects an index at random from the source and returns the value from it.
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The value selected.</returns>
	public static T RandomSelectOneExcept<T>(
		this in Span<T> source,
		Random? random,
		T excluding, params T[] others)
		=> RandomSelectOneExcept((ReadOnlySpan<T>)source, random, excluding, others);

	/// <summary>
	/// Selects an index at random from the source and returns the value from it.
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source span.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The value selected.</returns>
	public static T RandomSelectOneExcept<T>(
		this in Span<T> source,
		T excluding, params T[] others)
		=> RandomSelectOneExcept((ReadOnlySpan<T>)source, default, excluding, others);

	/// <summary>
	/// Selects an index at random from the source and returns the value from it.
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="random">The optional source of random numbers.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The value selected.</returns>
	public static T RandomSelectOneExcept<T>(
		this IReadOnlyCollection<T> source,
		Random? random,
		T excluding, params T[] others)
		=> source.Count == 0
			? throw new InvalidOperationException("Source collection is empty.")
			: source.TryRandomSelectOneExcept(out T value, random, excluding, others)
			? value
			: throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");

	/// <summary>
	/// Selects an index at random from the source and returns the value from it.
	/// Will not select indexes that are contained in the optional exclusion set.
	/// </summary>
	/// <typeparam name="T">The generic type of the source.</typeparam>
	/// <param name="source">The source collection.</param>
	/// <param name="excluding">The value to exclude from selection.</param>
	/// <param name="others">The additional set of optional values to exclude from selection.</param>
	/// <returns>The value selected.</returns>
	public static T RandomSelectOneExcept<T>(
		this IReadOnlyCollection<T> source,
		T excluding, params T[] others)
		=> RandomSelectOneExcept(source, default, excluding, others);

	/// <summary>
	/// Select a random number except the excluded ones.
	/// </summary>
	/// <param name="range">The range of values to select from.</param>
	/// <param name="exclusion">The values to exclude.</param>
	/// <returns>The value selected.</returns>
	public static ushort NextExcluding(this Random source,
		ushort range,
		IEnumerable<ushort> exclusion)
	{
		if (range == 0)
			throw new ArgumentOutOfRangeException(nameof(range), range, "Must be a number greater than zero.");

		DeferredHashSet<ushort>? setCreated = null;
		try
		{
			var exclusionSet = exclusion == null ? null
				: exclusion as ISet<ushort> ?? (setCreated = new DeferredHashSet<ushort>(exclusion));

			if (exclusionSet == null || exclusionSet.Count == 0)
				return (ushort)source.Next(range);

			var pool = ArrayPool<ushort>.Shared;
			var indexes = pool.Rent(range);
			try
			{
				var indexCount = 0;
				for (ushort i = 0; i < range; ++i)
				{
					if (!exclusionSet.Contains(i))
						indexes[indexCount++] = i;
				}

				return indexCount == 0
					? throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.")
					: indexes[source.Next(indexCount)];
			}
			finally
			{
				pool.Return(indexes);
			}
		}
		finally
		{
			setCreated?.Dispose();
		}
	}

	/// <summary>
	/// Select a random number except the excluded ones.
	/// </summary>
	/// <param name="range">The range of values to select from.</param>
	/// <param name="exclusion">The values to exclude.</param>
	/// <returns>The value selected.</returns>
	public static int NextExcluding(this Random source,
		int range,
		IEnumerable<int> exclusion)
	{
		if (range <= 0)
			throw new ArgumentOutOfRangeException(nameof(range), range, "Must be a number greater than zero.");

		DeferredHashSet<int>? setCreated = null;
		try
		{
			var exclusionSet = exclusion == null ? null
				: exclusion as ISet<int> ?? (setCreated = new DeferredHashSet<int>(exclusion));

			if (exclusionSet == null || exclusionSet.Count == 0)
				return source.Next(range);

			var pool = ArrayPool<int>.Shared;
			var indexes = pool.Rent(range);
			try
			{
				var indexCount = 0;
				for (var i = 0; i < range; ++i)
				{
					if (!exclusionSet.Contains(i))
						indexes[indexCount++] = i;
				}

				return indexCount == 0
					? throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.")
					: indexes[source.Next(indexCount)];
			}
			finally
			{
				pool.Return(indexes);
			}
		}
		finally
		{
			setCreated?.Dispose();
		}
	}

	/// <summary>
	/// Select a random number but skip the excluded one.
	/// </summary>
	/// <param name="range">The range of values to select from.</param>
	/// <param name="excluding">The value to skip.</param>
	/// <returns>The value selected.</returns>
	public static int NextExcluding(this Random source,
		int range,
		int excluding, params int[] others)
	{
		if (range <= 0)
			throw new ArgumentOutOfRangeException(nameof(range), range, "Must be a number greater than zero.");

		if (others.Length != 0)
			return source.NextExcluding(range, others.Prepend(excluding));

		if (excluding >= range || excluding < 0)
			return source.Next(range);

		if (excluding == 0 && range == 1)
			throw new ArgumentException("No value is available with a range of 1 and exclusion of 0.", nameof(range));

		var i = source.Next(range - 1);
		return i < excluding ? i : i + 1;
	}

	/// <summary>
	/// Select a random number but skip the excluded one.
	/// </summary>
	/// <param name="range">The range of values to select from.</param>
	/// <param name="excluding">The value to skip.</param>
	/// <returns>The value selected.</returns>
	public static int NextExcluding(this Random source,
		int range,
		uint excluding, params uint[] others)
	{
		var exInt = excluding > int.MaxValue ? -1 : (int)excluding;
		return others.Length == 0
			? NextExcluding(source, range, exInt)
			: NextExcluding(source, range,
				others.Where(v => v <= int.MaxValue).Cast<int>().Prepend(exInt));
	}
}
