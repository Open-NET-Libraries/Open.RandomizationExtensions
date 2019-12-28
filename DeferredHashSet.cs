using System;
using System.Collections.Generic;
namespace Open.RandomizationExtensions
{
	class DeferredHashSet<T> : HashSet<T>, IDisposable
	{
		public DeferredHashSet(IEnumerator<T> source)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
		}

		public DeferredHashSet(IEnumerable<T> source)
			: this((source ?? throw new ArgumentNullException(nameof(source))).GetEnumerator())
		{

		}

		private readonly IEnumerator<T> Source;

		public new bool Contains(T item)
		{
			if (base.Contains(item))
				return true;

			while(Source.MoveNext())
			{
				var i = Source.Current;
				Add(i);
				if (item is null ? i is null : item.Equals(i))
					return true;
			}

			return false;
		}

		public void Dispose()
		{
			Source.Dispose();
			this.Clear();
		}
	}
}
