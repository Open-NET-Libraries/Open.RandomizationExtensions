/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Genetic-Algorithm-Platform/blob/master/LICENSE.md
 */

using Open.Memory;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Open.RandomizationExtensions
{
	public static class Extensions
	{
		static readonly Lazy<Random> R = new Lazy<Random>(() => new Random());

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
		/// <returns>True if successfully retrieved and item and removed the node.</returns>
		[SuppressMessage("ReSharper", "PossibleNullReferenceException")]
		public static bool TryRandomPluck<T>(this LinkedList<T> source, out T value)
		{
			if (source.Count == 0)
			{
				value = default;
				return false;
			}

			var r = R.Value.Next(source.Count);
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
		/// <returns>The value retrieved.</returns>
		public static T RandomPluck<T>(this LinkedList<T> source)
		{
			if (source.TryRandomPluck(out var value))
				return value;

			throw new InvalidOperationException("Source collection is empty.");
		}

		/// <summary>
		/// Attempts to select an index at random and remove it.
		/// </summary>
		/// <typeparam name="T">The generic type of the list.</typeparam>
		/// <param name="source">The source list.</param>
		/// <param name="value">The value removed.</param>
		/// <returns>True if successfully removed.</returns>
		public static bool TryRandomPluck<T>(this List<T> source, out T value)
		{
			if (source.Count == 0)
			{
				value = default;
				return false;
			}

			var r = R.Value.Next(source.Count);
			value = source[r];
			source.RemoveAt(r);
			return true;
		}

		/// <summary>
		/// Selects an index at random, removes it, and returns its value.
		/// </summary>
		/// <typeparam name="T">The generic type of the list.</typeparam>
		/// <param name="source">The source list.</param>
		/// <returns>The value retrieved.</returns>
		public static T RandomPluck<T>(this List<T> source)
		{
			if (source.TryRandomPluck(out var value))
				return value;

			throw new InvalidOperationException("Source collection is empty.");
		}

		/// <summary>
		/// Randomly selects a reference from the source.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source span.</param>
		/// <returns>The reference selected.</returns>
		public static ref readonly T RandomSelectOne<T>(
			this in ReadOnlySpan<T> source)
		{
			if (source.Length == 0)
				throw new InvalidOperationException("Source collection is empty.");

			return ref source[R.Value.Next(source.Length)];
		}

		/// <summary>
		/// Randomly selects a reference from the source.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source span.</param>
		/// <returns>The reference selected.</returns>
		public static ref T RandomSelectOne<T>(
			this in Span<T> source)
		{
			if (source.Length == 0)
				throw new InvalidOperationException("Source collection is empty.");

			return ref source[R.Value.Next(source.Length)];
		}

		/// <summary>
		/// Randomly selects an index from the source.
		/// Will not return indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source span.</param>
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>The index selected.</returns>
		public static int RandomSelectIndex<T>(this in ReadOnlySpan<T> source, IEnumerable<T> exclusion = null)
		{
			if (source.Length == 0)
				return -1;

			HashSet<T> setCreated = null;
			try
			{
				var exclusionSet = exclusion == null ? default
					: exclusion as ISet<T> ?? (setCreated = new HashSet<T>(exclusion));

				if (exclusionSet == null || exclusionSet.Count == 0)
					return R.Value.Next(source.Length);

				var count = source.Length;
				using (var indexesTemp = ArrayPool<int>.Shared.RentDisposable(count))
				{
					var indexes = indexesTemp.Array;
					var indexCount = 0;
					for (var i = 0; i < count; ++i)
					{
						if (!exclusionSet.Contains(source[i]))
							indexes[indexCount++] = i;
					}
					return indexCount == 0 ? -1 : indexes[R.Value.Next(indexCount)];
				}
			}
			finally
			{
				setCreated?.Clear();
			}
		}

		/// <summary>
		/// Randomly selects an index from the source.
		/// Will not return indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source span.</param>
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>The index selected.</returns>
		public static int RandomSelectIndex<T>(this in Span<T> source, IEnumerable<T> exclusion = null)
			=> RandomSelectIndex((ReadOnlySpan<T>)source, exclusion);

		static int RandomSelectIndex<T>(int count, IEnumerable<T> source, IEnumerable<T> exclusion)
		{
			if (count == 0)
				return -1;

			HashSet<T> setCreated = null;
			try
			{
				var exclusionSet = exclusion == null ? default
				: exclusion as ISet<T> ?? (setCreated = new HashSet<T>(exclusion));

				if (exclusionSet == null || exclusionSet.Count == 0)
					return R.Value.Next(count);

				using (var indexesTemp = ArrayPool<int>.Shared.RentDisposable(count))
				{
					var indexes = indexesTemp.Array;
					var i = -1;
					var indexCount = 0;
					foreach (var value in source)
					{
						++i;
						if (!exclusionSet.Contains(value))
							indexes[indexCount++] = i;
					}
					return indexCount == 0 ? -1 : indexes[R.Value.Next(indexCount)];
				}
			}
			finally
			{
				setCreated?.Clear();
			}
		}

		/// <summary>
		/// Randomly selects an index from the source.
		/// Will not return indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source collection.</param>
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>The index selected.</returns>
		public static int RandomSelectIndex<T>(this IReadOnlyCollection<T> source, IEnumerable<T> exclusion = null)
			=> RandomSelectIndex(source.Count, source, exclusion);

		/// <summary>
		/// Randomly selects an index from the source.
		/// Will not return indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source collection.</param>
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>The index selected.</returns>
		public static int RandomSelectIndex<T>(this ICollection<T> source, IEnumerable<T> exclusion = null)
			=> RandomSelectIndex(source.Count, source, exclusion);

		/// <summary>
		/// Randomly selects an index from the source.
		/// Will not return indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source span.</param>
		/// <param name="exclusion">A value to exclude from selection.</param>
		/// <param name="others">The additional set of optional values to exclude from selection.</param>
		/// <returns>The index selected.</returns>
		public static int RandomSelectIndexExcept<T>(this in ReadOnlySpan<T> source, T exclusion, params T[] others)
		{
			if (source.Length == 0)
				return -1;

			if (others.Length != 0)
			{
				var setCreated = new HashSet<T>(others) { exclusion };
				try
				{
					return RandomSelectIndex(in source, setCreated);
				}
				finally
				{
					setCreated.Clear();
				}
			}

			var count = source.Length;
			using (var indexesTemp = ArrayPool<int>.Shared.RentDisposable(count))
			{
				var indexes = indexesTemp.Array;
				var indexCount = 0;
				for (var i = 0; i < count; i++)
				{
					if (!exclusion.Equals(source[i]))
						indexes[indexCount++] = i;
				}
				return indexCount == 0 ? -1 : indexes[R.Value.Next(indexCount)];
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
		public static int RandomSelectIndexExcept<T>(this in Span<T> source, T exclusion, params T[] others)
			=> RandomSelectIndexExcept((ReadOnlySpan<T>)source, exclusion, others);

		static int RandomSelectIndexExcept<T>(int count, IEnumerable<T> source, T exclusion, T[] others)
		{
			if (count == 0)
				return -1;

			if (others.Length != 0)
			{
				var setCreated = new HashSet<T>(others) { exclusion };
				try
				{
					return RandomSelectIndex(count, source, setCreated);
				}
				finally
				{
					setCreated.Clear();
				}
			}

			using (var indexesTemp = ArrayPool<int>.Shared.RentDisposable(count))
			{
				var indexes = indexesTemp.Array;
				var i = -1;
				var indexCount = 0;
				foreach (var value in source)
				{
					++i;
					if (!exclusion.Equals(value))
						indexes[indexCount++] = i;
				}
				return indexCount == 0 ? -1 : indexes[R.Value.Next(indexCount)];
			}
		}

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
			=> RandomSelectIndexExcept(source.Count, source, exclusion, others);

		/// <summary>
		/// Randomly selects an index from the source.
		/// Will not return indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source collection.</param>
		/// <param name="exclusion">A value to exclude from selection.</param>
		/// <param name="others">The additional set of optional values to exclude from selection.</param>
		/// <returns>The index selected.</returns>
		public static int RandomSelectIndexExcept<T>(this ICollection<T> source, T exclusion, params T[] others)
			=> RandomSelectIndexExcept(source.Count, source, exclusion, others);

		/// <summary>
		/// Attempts to select an index at random from the source and returns the value from it..
		/// Will not select indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source span.</param>
		/// <param name="value">The value selected.</param>
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>True if a valid value was selected.</returns>
		public static bool TryRandomSelectOne<T>(
			this in ReadOnlySpan<T> source,
			out T value,
			IEnumerable<T> exclusion = null)
		{
			var index = RandomSelectIndex(in source, exclusion);
			if (index == -1)
			{
				value = default;
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
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>True if a valid value was selected.</returns>
		public static bool TryRandomSelectOne<T>(
			this in Span<T> source,
			out T value,
			IEnumerable<T> exclusion = null)
			=> TryRandomSelectOne((ReadOnlySpan<T>)source, out value, exclusion);

		/// <summary>
		/// Selects an index at random from the source and returns the value from it.
		/// Will not select indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source span.</param>
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>The value selected.</returns>
		public static T RandomSelectOne<T>(
			this IReadOnlyCollection<T> source,
			IEnumerable<T> exclusion = null)
		{
			if (source.Count == 0)
				throw new InvalidOperationException("Source collection is empty.");

			var index = RandomSelectIndex(source, exclusion);
			if (index == -1)
				throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");

			return source is IReadOnlyList<T> list
				? list[index]
				: source.ElementAt(index);
		}

		/// <summary>
		/// Selects an index at random from the source and returns the value from it.
		/// Will not select indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source span.</param>
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>The value selected.</returns>
		public static T RandomSelectOne<T>(
			this ICollection<T> source,
			IEnumerable<T> exclusion = null)
		{
			if (source.Count == 0)
				throw new InvalidOperationException("Source collection is empty.");

			var index = RandomSelectIndex(source, exclusion);
			if (index == -1)
				throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");

			return source is IList<T> list
				? list[index]
				: source.ElementAt(index);
		}

		/// <summary>
		/// Attempts to select an index at random from the source and returns the value from it..
		/// Will not select indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source collection.</param>
		/// <param name="value">The value selected.</param>
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>True if a valid value was selected.</returns>
		public static bool TryRandomSelectOne<T>(
			this IReadOnlyCollection<T> source,
			out T value,
			IEnumerable<T> exclusion = null)
		{
			var index = RandomSelectIndex(source, exclusion);
			if (index == -1)
			{
				value = default;
				return false;
			}

			value = source is IReadOnlyList<T> list
				? list[index]
				: source.ElementAt(index);

			return true;
		}

		/// <summary>
		/// Attempts to select an index at random from the source and returns the value from it..
		/// Will not select indexes that are contained in the optional exclusion set.
		/// </summary>
		/// <typeparam name="T">The generic type of the source.</typeparam>
		/// <param name="source">The source collection.</param>
		/// <param name="value">The value selected.</param>
		/// <param name="exclusion">The optional values to exclude from selection.</param>
		/// <returns>True if a valid value was selected.</returns>
		public static bool TryRandomSelectOne<T>(
			this ICollection<T> source,
			out T value,
			IEnumerable<T> exclusion = null)
		{
			var index = RandomSelectIndex(source, exclusion);
			if (index == -1)
			{
				value = default;
				return false;
			}

			value = source is IList<T> list
				? list[index]
				: source.ElementAt(index);

			return true;
		}

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
			this in ReadOnlySpan<T> source,
			out T value,
			T excluding, params T[] others)
		{
			var index = RandomSelectIndexExcept(in source, excluding, others);
			if (index == -1)
			{
				value = default;
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
		/// <param name="excluding">The optional values to exclude from selection.</param>
		/// <param name="others">The additional set of optional values to exclude from selection.</param>
		/// <returns>True if a valid value was selected.</returns>
		public static bool TryRandomSelectOneExcept<T>(
			this in Span<T> source,
			out T value,
			T excluding, params T[] others)
			=> TryRandomSelectOneExcept((ReadOnlySpan<T>)source, out value, excluding, others);

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
		{
			var index = RandomSelectIndexExcept(source, excluding, others);
			if (index == -1)
			{
				value = default;
				return false;
			}

			value = source is IReadOnlyList<T> list
				? list[index]
				: source.ElementAt(index);

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
			this ICollection<T> source,
			out T value,
			T excluding, params T[] others)
		{
			var index = RandomSelectIndexExcept(source, excluding, others);
			if (index == -1)
			{
				value = default;
				return false;
			}

			value = source is IList<T> list
				? list[index]
				: source.ElementAt(index);

			return true;
		}

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
		{
			if (source.Length == 0)
				throw new InvalidOperationException("Source collection is empty.");

			if (TryRandomSelectOneExcept(in source, out T value, excluding, others))
				return value;

			throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");
		}

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
			=> RandomSelectOneExcept((ReadOnlySpan<T>)source, excluding, others);

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
		{
			if (source.Count == 0)
				throw new InvalidOperationException("Source collection is empty.");

			if (source.TryRandomSelectOneExcept(out T value, excluding, others))
				return value;

			throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");
		}

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
			this ICollection<T> source,
			T excluding, params T[] others)
		{
			if (source.Count == 0)
				throw new InvalidOperationException("Source collection is empty.");

			if (source.TryRandomSelectOneExcept(out T value, excluding, others))
				return value;

			throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");
		}

		/// <summary>
		/// Select a random number except the excluded ones.
		/// </summary>
		/// <param name="range">The range of values to select from.</param>
		/// <param name="excluding">The values to exclude.</param>
		/// <returns>The value selected.</returns>
		public static ushort NextRandomIntegerExcluding(
			ushort range,
			IEnumerable<ushort> exclusion)
		{
			if (range == 0)
				throw new ArgumentOutOfRangeException(nameof(range), range, "Must be a number greater than zero.");

			HashSet<ushort> setCreated = null;
			try
			{
				var exclusionSet = exclusion == null ? null
				: exclusion as ISet<ushort> ?? (setCreated = new HashSet<ushort>(exclusion));

				if (exclusionSet == null || exclusionSet.Count == 0)
					return (ushort)R.Value.Next(range);

				using (var indexesTemp = ArrayPool<ushort>.Shared.RentDisposable(range))
				{
					var indexes = indexesTemp.Array;

					var indexCount = 0;
					for (ushort i = 0; i < range; ++i)
					{
						if (!exclusionSet.Contains(i))
							indexes[indexCount++] = i;
					}
					if (indexCount == 0)
						throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");

					return indexes[R.Value.Next(indexCount)];
				}
			}
			finally
			{
				setCreated?.Clear();
			}
		}

		/// <summary>
		/// Select a random number except the excluded ones.
		/// </summary>
		/// <param name="range">The range of values to select from.</param>
		/// <param name="excluding">The values to exclude.</param>
		/// <returns>The value selected.</returns>
		public static int NextRandomIntegerExcluding(
			int range,
			IEnumerable<int> exclusion)
		{
			if (range <= 0)
				throw new ArgumentOutOfRangeException(nameof(range), range, "Must be a number greater than zero.");

			HashSet<int> setCreated = null;
			try
			{
				var exclusionSet = exclusion == null ? null
				: exclusion as ISet<int> ?? (setCreated = new HashSet<int>(exclusion));

				if (exclusionSet == null || exclusionSet.Count == 0)
					return R.Value.Next(range);

				using (var indexesTemp = ArrayPool<int>.Shared.RentDisposable(range))
				{
					var indexes = indexesTemp.Array;
					var indexCount = 0;
					for (var i = 0; i < range; ++i)
					{
						if (!exclusionSet.Contains(i))
							indexes[indexCount++] = i;
					}
					if (indexCount == 0)
						throw new InvalidOperationException("Exclusion set invalidates the source.  No possible value can be selected.");
					return indexes[R.Value.Next(indexCount)];
				}
			}
			finally
			{
				setCreated?.Clear();
			}
		}

		/// <summary>
		/// Select a random number but skip the excluded one.
		/// </summary>
		/// <param name="range">The range of values to select from.</param>
		/// <param name="excluding">The value to skip.</param>
		/// <returns>The value selected.</returns>
		public static int NextRandomIntegerExcluding(
			int range,
			int excluding)
		{
			if (range <= 0)
				throw new ArgumentOutOfRangeException(nameof(range), range, "Must be a number greater than zero.");

			if (excluding >= range || excluding < 0)
				return R.Value.Next(range);

			if (excluding == 0 && range == 1)
				throw new ArgumentException("No value is available with a range of 1 and exclusion of 0.", "range");

			var i = R.Value.Next(range - 1);
			if (i >= excluding)
				return i + 1;

			return i;
		}

		/// <summary>
		/// Select a random number but skip the excluded one.
		/// </summary>
		/// <param name="range">The range of values to select from.</param>
		/// <param name="excluding">The value to skip.</param>
		/// <returns>The value selected.</returns>
		public static int NextRandomIntegerExcluding(
			int range,
			uint excluding)
			=> NextRandomIntegerExcluding(range, excluding > int.MaxValue ? -1 : (int)excluding);

	}

}
