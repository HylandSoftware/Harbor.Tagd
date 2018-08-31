using Harbor.Tagd.API.Models;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Harbor.Tagd
{
	internal class ConcurrentTagSet : IEnumerable<Tag>
	{
		private readonly ConcurrentDictionary<Tag, byte> _storage = new ConcurrentDictionary<Tag, byte>();

		public void Remove(Tag t) => _storage.Remove(t, out var _);

		public void Add(Tag t) => _storage.TryAdd(t, 0);

		public void Clear() => _storage.Clear();

		public IEnumerator<Tag> GetEnumerator() => _storage.Keys.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _storage.Keys.GetEnumerator();

		public void UnionWith(ConcurrentTagSet other)
		{
			foreach(var t in other)
			{
				_storage.TryAdd(t, 0);
			}
		}

		public int Count => _storage.Count;
	}

	internal class TagSet
	{
		public ConcurrentTagSet Tags { get; } = new ConcurrentTagSet();
		public ConcurrentTagSet ToKeep { get; } = new ConcurrentTagSet();
		public ConcurrentTagSet ToRemove { get; } = new ConcurrentTagSet();
	}
}
