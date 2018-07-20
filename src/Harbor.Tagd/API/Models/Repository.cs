using Newtonsoft.Json;
using System;

namespace Harbor.Tagd.API.Models
{
	public static class RepoExtensions
	{
		public static string[] ValidateAndSplit(this string repo)
		{
			if (!repo.Contains("/"))
			{
				throw new ArgumentException($"Illegal Repository Path: {repo}", nameof(repo));
			}

			return repo.Split('/');
		}
	}

	public class Repository : IEquatable<Repository>
	{
		public int Id { get; set; }
		public string Name { get; set; }

		[JsonProperty("project_id")]
		public int ProjectId { get; set; }

		[JsonProperty("tags_count")]
		public int TagCount { get; set; }

		public bool Equals(Repository other) =>
			other != null &&
			Id == other.Id &&
			Name == other.Name &&
			ProjectId == other.ProjectId &&
			TagCount == other.TagCount;
	}
}
