using Open.RandomizationExtensions;

namespace Open.RandomizationExtensions.Tests;

/// <summary>
/// Regression tests from the adversarial review of the 2.6.0 stack.
/// </summary>
public class ReviewFindingTests
{
	private const int Iterations = 500;

	/// <summary>
	/// The span RandomSelectIndex Count==1 fast path delegates to RandomSelectIndexExcept
	/// with no 'others' -- a call shape whose overload binding silently shifted to the new
	/// params-span overload on net10. This pins the behavior contract for that exact shape
	/// so the two implementations cannot silently diverge.
	/// </summary>
	[Fact]
	public void Span_RandomSelectIndex_SingleElementSetExclusion_FastPathHonorsExclusion()
	{
		var exclusion = new HashSet<int> { 20 };
		var seen = new HashSet<int>();
		for (var i = 0; i < Iterations; i++)
		{
			ReadOnlySpan<int> source = [10, 20, 30];
			var index = source.RandomSelectIndex(exclusion: exclusion);
			Assert.True(index is 0 or 2, $"selected excluded index {index}");
			seen.Add(index);
		}

		Assert.Equal([0, 2], seen.Order());
	}

	[Fact]
	public void Span_TryRandomSelectOne_SingleElementSetExclusion_NeverSelectsExcluded()
	{
		var exclusion = new HashSet<char> { 'b' };
		for (var i = 0; i < Iterations; i++)
		{
			ReadOnlySpan<char> source = ['a', 'b', 'c'];
			Assert.True(source.TryRandomSelectOne(out var value, exclusion));
			Assert.NotEqual('b', value);
		}
	}

	/// <summary>
	/// The headline 2.6.0 claim is a thread-safe default RNG. This exercises the default
	/// path from many threads concurrently: no exception, all draws in range, and the
	/// output is not degenerate (the classic corruption mode of a shared System.Random
	/// is collapsing to constant output).
	/// </summary>
	[Fact]
	public async Task DefaultRng_ParallelDraws_NoCorruptionOrException()
	{
		var results = new int[64][];
		await Task.WhenAll(Enumerable.Range(0, 64).Select(t => Task.Run(() =>
		{
			var local = new int[1000];
			var source = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			for (var i = 0; i < local.Length; i++)
				local[i] = source.RandomSelectOne();
			results[t] = local;
		})));

		var all = results.SelectMany(x => x).ToArray();
		Assert.All(all, v => Assert.InRange(v, 0, 9));
		// 64,000 draws from 10 values: seeing fewer than 10 distinct values would
		// indicate a corrupted/constant generator.
		Assert.Equal(10, all.Distinct().Count());
	}

	[Fact]
	public void DefaultRandomProperty_IsUsableAndDrawsVary()
	{
		var random = Randomizer.Random;
		var draws = Enumerable.Range(0, 256).Select(_ => random.Next(1000)).ToArray();
		Assert.True(draws.Distinct().Count() > 1, "default Random produced constant output");
	}
}
