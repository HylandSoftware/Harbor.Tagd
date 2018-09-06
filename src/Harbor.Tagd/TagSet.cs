using Harbor.Tagd.API.Models;
using System.Collections.Generic;

namespace Harbor.Tagd
{
	internal class TagSet
	{
		public HashSet<Tag> Tags { get; } = new HashSet<Tag>();
		public HashSet<Tag> ToKeep { get; } = new HashSet<Tag>();
		public HashSet<Tag> ToRemove { get; } = new HashSet<Tag>();
	}
}
