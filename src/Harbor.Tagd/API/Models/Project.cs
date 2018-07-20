using Newtonsoft.Json;
using System;

namespace Harbor.Tagd.API.Models
{
	public class Project : IEquatable<Project>
	{
		[JsonProperty("project_id")]
		public int Id { get; set; }

		public string Name { get; set; }

		public bool Equals(Project other) =>
			other != null &&
			Id == other.Id &&
			Name == other.Name;
	}
}
