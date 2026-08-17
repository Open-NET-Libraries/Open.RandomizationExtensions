using Open.RandomizationExtensions;

namespace Open.RandomizationExtensions.Tests;

/// <summary>
/// Coverage for the net10.0 params-ReadOnlySpan overloads, plus explicit-array calls
/// that pin the classic params T[] overloads (literal arguments bind the span forms
/// under C# 13 preference, so the array forms need explicitly-typed arrays to stay
/// exercised).
/// </summary>
public class SpanParamsOverloadTests
{
	private const int Iterations = 500;

	// ---- Span-params forms (literal arguments bind these). ----

	[Fact]
	public void Collection_SpanParams_TryRandomSelectOneExcept_HonorsAllExclusions()
	{
		IReadOnlyCollection<char> source = ['a', 'b', 'c'];
		for (var i = 0; i < Iterations; i++)
		{
			Assert.True(source.TryRandomSelectOneExcept(out var value, 'a', 'b'));
			Assert.Equal('c', value);
		}
	}

	[Fact]
	public void Collection_SpanParams_AllExcluded_ReturnsFalse()
	{
		IReadOnlyCollection<char> source = ['a', 'b'];
		Assert.False(source.TryRandomSelectOneExcept(out _, 'a', 'b'));
	}

	[Fact]
	public void Span_SpanParams_TryRandomSelectOneExcept_HonorsAllExclusions()
	{
		ReadOnlySpan<char> source = ['a', 'b', 'c', 'd'];
		for (var i = 0; i < Iterations; i++)
		{
			Assert.True(source.TryRandomSelectOneExcept(out var value, 'a', 'b', 'c'));
			Assert.Equal('d', value);
		}
	}

	[Fact]
	public void Span_SpanParams_RandomSelectIndexExcept_NeverSelectsExcluded()
	{
		var seen = new HashSet<int>();
		for (var i = 0; i < Iterations; i++)
		{
			ReadOnlySpan<int> source = [10, 20, 30, 40];
			var index = source.RandomSelectIndexExcept(20, 40);
			Assert.True(index is 0 or 2);
			seen.Add(index);
		}

		Assert.Equal([0, 2], seen.Order());
	}

	[Fact]
	public void Collection_SpanParams_RandomSelectOneExcept_AllExcluded_Throws()
	{
		IReadOnlyCollection<char> source = ['a', 'b'];
		Assert.Throws<InvalidOperationException>(() => source.RandomSelectOneExcept('a', 'b'));
	}

	[Fact]
	public void NextExcluding_SpanParams_Excludes()
	{
		var random = new Random(11);
		for (var i = 0; i < Iterations; i++)
		{
			var value = random.NextExcluding(5, 0, 2, 4);
			Assert.True(value is 1 or 3);
		}
	}

	[Fact]
	public void NextExcluding_SpanParams_AllExcluded_Throws()
	{
		var random = new Random(11);
		Assert.Throws<InvalidOperationException>(() => random.NextExcluding(2, 0, 1));
	}

	[Fact]
	public void SpanParams_SuppliedRandom_IsDeterministic()
	{
		static int[] Draw(int seed)
		{
			var random = new Random(seed);
			var picks = new int[64];
			for (var i = 0; i < picks.Length; i++)
			{
				ReadOnlySpan<int> src = [1, 2, 3, 4, 5];
				picks[i] = src.RandomSelectOneExcept(random, 2, 4);
			}

			return picks;
		}

		Assert.Equal(Draw(99), Draw(99));
		Assert.All(Draw(99), v => Assert.True(v is 1 or 3 or 5));
	}

	// ---- Explicit arrays: keep the classic params T[] overloads exercised. ----

	[Fact]
	public void Collection_ArrayParams_TryRandomSelectOneExcept_HonorsAllExclusions()
	{
		IReadOnlyCollection<char> source = ['a', 'b', 'c'];
		var others = new[] { 'b' };
		for (var i = 0; i < Iterations; i++)
		{
			Assert.True(source.TryRandomSelectOneExcept(out var value, 'a', others));
			Assert.Equal('c', value);
		}
	}

	[Fact]
	public void Span_ArrayParams_TryRandomSelectOneExcept_HonorsAllExclusions()
	{
		var others = new[] { 'b' };
		for (var i = 0; i < Iterations; i++)
		{
			ReadOnlySpan<char> source = ['a', 'b', 'c'];
			Assert.True(source.TryRandomSelectOneExcept(out var value, 'a', others));
			Assert.Equal('c', value);
		}
	}

	[Fact]
	public void NextExcluding_ArrayParams_Excludes()
	{
		var random = new Random(11);
		var others = new[] { 2 };
		for (var i = 0; i < Iterations; i++)
		{
			var value = random.NextExcluding(4, 0, others);
			Assert.True(value is 1 or 3);
		}
	}
}
