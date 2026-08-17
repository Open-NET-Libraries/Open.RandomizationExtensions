using Open.RandomizationExtensions;

namespace Open.RandomizationExtensions.Tests;

/// <summary>
/// Regression coverage for the exclusion-based selection paths. Prior to 2.5.4:
/// any lazily-materialized exclusion (anything not already an <see cref="ISet{T}"/>)
/// was silently ignored, because <c>DeferredHashSet</c> fills itself during
/// <c>Contains</c> but the call sites consulted <c>Count</c> before anything had been
/// pumped; and the span-based single-exclusion path rented a zero-length pooled array
/// and threw <see cref="IndexOutOfRangeException"/> for any non-empty source.
/// </summary>
public class ExclusionSelectionTests
{
	private const int Iterations = 500;

	// ---- The reported defect: params 'others' were ignored (collection path). ----

	[Fact]
	public void Collection_TryRandomSelectOneExcept_HonorsAllExclusions()
	{
		IReadOnlyCollection<char> source = new[] { 'a', 'b', 'c' };
		for (var i = 0; i < Iterations; i++)
		{
			Assert.True(source.TryRandomSelectOneExcept(out var value, 'a', 'b'));
			Assert.Equal('c', value);
		}
	}

	[Fact]
	public void Collection_TryRandomSelectOneExcept_AllExcluded_ReturnsFalse()
	{
		IReadOnlyCollection<char> source = new[] { 'a', 'b' };
		Assert.False(source.TryRandomSelectOneExcept(out _, 'a', 'b'));
	}

	[Fact]
	public void Collection_RandomSelectOneExcept_AllExcluded_Throws()
	{
		IReadOnlyCollection<char> source = new[] { 'a', 'b' };
		Assert.Throws<InvalidOperationException>(() => source.RandomSelectOneExcept('a', 'b'));
	}

	// ---- Same defect through the span overloads. ----

	[Fact]
	public void Span_TryRandomSelectOneExcept_HonorsAllExclusions()
	{
		ReadOnlySpan<char> source = ['a', 'b', 'c'];
		for (var i = 0; i < Iterations; i++)
		{
			Assert.True(source.TryRandomSelectOneExcept(out var value, 'a', 'b'));
			Assert.Equal('c', value);
		}
	}

	// ---- The span single-exclusion crash (Rent(0)). ----

	[Fact]
	public void Span_SingleExclusion_DoesNotThrowAndNeverSelectsExcluded()
	{
		ReadOnlySpan<char> source = ['a', 'b', 'c'];
		for (var i = 0; i < Iterations; i++)
		{
			// Pre-2.5.4 this threw IndexOutOfRangeException on every call.
			Assert.True(source.TryRandomSelectOneExcept(out var value, 'b'));
			Assert.NotEqual('b', value);
		}
	}

	[Fact]
	public void Span_RandomSelectIndexExcept_SingleExclusion_CoversAllRemaining()
	{
		ReadOnlySpan<int> source = [10, 20, 30, 40];
		var seen = new HashSet<int>();
		for (var i = 0; i < Iterations; i++)
		{
			var index = source.RandomSelectIndexExcept(20);
			Assert.InRange(index, 0, 3);
			Assert.NotEqual(1, index);
			seen.Add(index);
		}

		// All non-excluded indexes remain reachable.
		Assert.Equal([0, 2, 3], seen.Order());
	}

	// ---- Enumerable (non-ISet) exclusion: the lazy-set path. ----

	[Fact]
	public void Collection_TryRandomSelectOne_WithEnumerableExclusion_HonorsIt()
	{
		IReadOnlyCollection<int> source = new[] { 1, 2, 3, 4 };
		// Deliberately NOT an ISet<T>: forces the DeferredHashSet path.
		IEnumerable<int> exclusion = new[] { 1, 2, 3 }.Where(_ => true);
		for (var i = 0; i < Iterations; i++)
		{
			Assert.True(source.TryRandomSelectOne(out var value, exclusion));
			Assert.Equal(4, value);
		}
	}

	[Fact]
	public void Span_TryRandomSelectOne_WithEnumerableExclusion_HonorsIt()
	{
		ReadOnlySpan<int> source = [1, 2, 3, 4];
		IEnumerable<int> exclusion = new[] { 4, 1 }.Where(_ => true);
		for (var i = 0; i < Iterations; i++)
		{
			Assert.True(source.TryRandomSelectOne(out var value, exclusion));
			Assert.True(value is 2 or 3, $"selected excluded value {value}");
		}
	}

	[Fact]
	public void Collection_TryRandomSelectOne_WithSetExclusion_StillWorks()
	{
		// The previously-working path: exclusion already an ISet<T>.
		IReadOnlyCollection<int> source = new[] { 1, 2, 3 };
		var exclusion = new HashSet<int> { 1, 3 };
		for (var i = 0; i < Iterations; i++)
		{
			Assert.True(source.TryRandomSelectOne(out var value, exclusion));
			Assert.Equal(2, value);
		}
	}

	[Fact]
	public void Collection_TryRandomSelectOne_ExclusionCoversSource_ReturnsFalse()
	{
		IReadOnlyCollection<int> source = new[] { 1, 2 };
		IEnumerable<int> exclusion = new[] { 1, 2, 99 }.Where(_ => true);
		Assert.False(source.TryRandomSelectOne(out _, exclusion));
	}

	// ---- Determinism: a supplied Random must actually be used. ----

	[Fact]
	public void SuppliedRandom_ProducesDeterministicSequence()
	{
		IReadOnlyCollection<int> source = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

		static int[] Draw(int seed)
		{
			var random = new Random(seed);
			IReadOnlyCollection<int> src = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
			var picks = new int[64];
			for (var i = 0; i < picks.Length; i++)
			{
				Assert.True(src.TryRandomSelectOneExcept(out var value, random, 3));
				picks[i] = value;
			}

			return picks;
		}

		Assert.Equal(Draw(1234), Draw(1234));
		Assert.All(Draw(1234), v => Assert.NotEqual(3, v));
	}

	[Fact]
	public void Span_SuppliedRandom_SingleExclusion_ProducesDeterministicSequence()
	{
		static int[] Draw(int seed)
		{
			var random = new Random(seed);
			var picks = new int[64];
			for (var i = 0; i < picks.Length; i++)
			{
				ReadOnlySpan<int> src = [10, 20, 30, 40];
				picks[i] = src.RandomSelectIndexExcept(random, 30);
			}

			return picks;
		}

		// Pre-2.5.4 this path drew from the global Random regardless of the argument.
		Assert.Equal(Draw(42), Draw(42));
	}
}
