using Newtonsoft.Json;
using System;

namespace Harbor.Tagd.API.Models
{
	public class Tag : IEquatable<Tag>
	{
		public string Digest { get; set; }

		public string Name { get; set; }

		[JsonProperty("created")]
		public DateTime CreatedAt { get; set; }

		[JsonIgnore]
		public string Repository { get; set; }

		public bool Equals(Tag other) =>
			other != null &&
			Digest == other.Digest &&
			Name == other.Name &&
			CreatedAt == other.CreatedAt &&
			Repository == other.Repository;
	}
}
