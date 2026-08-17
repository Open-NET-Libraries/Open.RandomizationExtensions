using Open.RandomizationExtensions;

namespace Open.RandomizationExtensions.Tests;

public class PluckAndNextExcludingTests
{
	private const int Iterations = 500;

	// ---- LinkedList pluck: off-by-one (never first, NRE on last). ----

	[Fact]
	public void LinkedList_TryRandomPluck_SingleElement_Succeeds()
	{
		// Pre-2.5.4 a single-element list threw NullReferenceException on EVERY draw.
		for (var i = 0; i < Iterations; i++)
		{
			var list = new LinkedList<int>([42]);
			Assert.True(list.TryRandomPluck(out var value));
			Assert.Equal(42, value);
			Assert.Empty(list);
		}
	}

	[Fact]
	public void LinkedList_TryRandomPluck_CanSelectFirstElement()
	{
		// Pre-2.5.4 the first element was unreachable (the walk always advanced past it).
		var seenFirst = false;
		for (var i = 0; i < Iterations && !seenFirst; i++)
		{
			var list = new LinkedList<char>(['a', 'b', 'c']);
			Assert.True(list.TryRandomPluck(out var value));
			seenFirst = value == 'a';
		}

		Assert.True(seenFirst, "first element was never selected");
	}

	[Fact]
	public void LinkedList_TryRandomPluck_DrainsCompletelyWithoutError()
	{
		var list = new LinkedList<int>(Enumerable.Range(0, 20));
		var drained = new List<int>();
		while (list.TryRandomPluck(out var value))
			drained.Add(value);

		Assert.Empty(list);
		Assert.Equal(Enumerable.Range(0, 20), drained.Order());
	}

	[Fact]
	public void LinkedList_TryRandomPluck_Empty_ReturnsFalse()
	{
		var list = new LinkedList<int>();
		Assert.False(list.TryRandomPluck(out _));
	}

	[Fact]
	public void List_TryRandomPluck_DrainsCompletelyWithoutError()
	{
		var list = new List<int>(Enumerable.Range(0, 20));
		var drained = new List<int>();
		while (list.TryRandomPluck(out var value))
			drained.Add(value);

		Assert.Empty(list);
		Assert.Equal(Enumerable.Range(0, 20), drained.Order());
	}

	// ---- NextExcluding: uint params overload (InvalidCastException). ----

	[Fact]
	public void NextExcluding_UIntParams_DoesNotThrowAndExcludes()
	{
		var random = new Random(7);
		for (var i = 0; i < Iterations; i++)
		{
			// Pre-2.5.4 this threw InvalidCastException (LINQ Cast<int> cannot unbox uint).
			var value = random.NextExcluding(5, 1u, 3u);
			Assert.InRange(value, 0, 4);
			Assert.NotEqual(1, value);
			Assert.NotEqual(3, value);
		}
	}

	[Fact]
	public void NextExcluding_IntParams_Excludes()
	{
		var random = new Random(7);
		for (var i = 0; i < Iterations; i++)
		{
			var value = random.NextExcluding(4, 0, 2);
			Assert.True(value is 1 or 3);
		}
	}

	[Fact]
	public void NextExcluding_SingleInt_SkipsExcluded()
	{
		var random = new Random(7);
		for (var i = 0; i < Iterations; i++)
			Assert.NotEqual(2, random.NextExcluding(5, 2));
	}

	[Fact]
	public void NextExcluding_EnumerableExclusion_Excludes()
	{
		var random = new Random(7);
		IEnumerable<int> exclusion = new[] { 0, 1, 2 }.Where(_ => true);
		for (var i = 0; i < Iterations; i++)
			Assert.Equal(3, random.NextExcluding(4, exclusion));
	}

	[Fact]
	public void NextExcluding_ExclusionCoversRange_Throws()
	{
		var random = new Random(7);
		Assert.Throws<InvalidOperationException>(
			() => random.NextExcluding(2, new[] { 0, 1 }.Where(_ => true)));
	}

	[Fact]
	public void NextExcluding_UShortRange_Excludes()
	{
		var random = new Random(7);
		IEnumerable<ushort> exclusion = new ushort[] { 0, 2 }.Where(_ => true);
		for (var i = 0; i < Iterations; i++)
		{
			var value = random.NextExcluding((ushort)3, exclusion);
			Assert.Equal(1, value);
		}
	}
}
