using Open.RandomizationExtensions;

namespace Open.RandomizationExtensions.Tests;

/// <summary>
/// The analyzer pass (CA1062) added ArgumentNullException guards to every externally
/// visible method that dereferences a reference argument. These pin the guard behavior
/// (ArgumentNullException with the right parameter name, not NullReferenceException).
/// </summary>
public class NullGuardTests
{
	[Fact]
	public void TryRandomPluck_NullLinkedList_ThrowsArgumentNull()
		=> Assert.Throws<ArgumentNullException>("source",
			() => ((LinkedList<int>)null!).TryRandomPluck(out _));

	[Fact]
	public void TryRandomPluck_NullList_ThrowsArgumentNull()
		=> Assert.Throws<ArgumentNullException>("source",
			() => ((List<int>)null!).TryRandomPluck(out _));

	[Fact]
	public void RandomSelectIndex_NullCollection_ThrowsArgumentNull()
		=> Assert.Throws<ArgumentNullException>("source",
			() => ((IReadOnlyCollection<int>)null!).RandomSelectIndex());

	[Fact]
	public void RandomSelectOne_NullCollection_ThrowsArgumentNull()
		=> Assert.Throws<ArgumentNullException>("source",
			() => ((IReadOnlyCollection<int>)null!).RandomSelectOne());

	[Fact]
	public void TryRandomSelectOneExcept_NullOthersArray_ThrowsArgumentNull()
	{
		IReadOnlyCollection<char> source = ['a', 'b'];
		Assert.Throws<ArgumentNullException>("others",
			() => source.TryRandomSelectOneExcept(out _, 'a', (char[])null!));
	}

	[Fact]
	public void NextExcluding_NullRandom_ThrowsArgumentNull()
		=> Assert.Throws<ArgumentNullException>("source",
			() => ((Random)null!).NextExcluding(4, new[] { 1 }.Where(_ => true)));
}
