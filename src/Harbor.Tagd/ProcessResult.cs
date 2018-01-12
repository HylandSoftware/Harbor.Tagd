using System;

namespace Harbor.Tagd
{
	public class ProcessResult : IEquatable<ProcessResult>
	{
		public readonly int RemovedTags;
		public readonly int IgnoredTags;

		public readonly int IgnoredRepos;

		public readonly int IgnoredProjects;

		public ProcessResult(int removedTags = 0, int ignoredTags = 0, int ignoredRepos = 0, int ignoredProjects = 0)
		{
			RemovedTags = removedTags;
			IgnoredTags = ignoredTags;
			IgnoredRepos = ignoredRepos;
			IgnoredProjects = ignoredProjects;
		}

		public bool Equals(ProcessResult other) =>
			other != null &&
			RemovedTags == other.RemovedTags &&
			IgnoredTags == other.IgnoredTags &&
			IgnoredRepos == other.IgnoredRepos &&
			IgnoredProjects == other.IgnoredProjects;
	}
}
