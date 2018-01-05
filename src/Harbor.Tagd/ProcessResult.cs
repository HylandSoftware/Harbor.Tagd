namespace Harbor.Tagd
{
	public class ProcessResult
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

		public static ProcessResult operator +(ProcessResult a, ProcessResult b) =>
			new ProcessResult(
				a.RemovedTags + b.RemovedTags,
				a.IgnoredTags + b.IgnoredTags,
				a.IgnoredRepos + b.IgnoredRepos,
				a.IgnoredProjects + b.IgnoredProjects
			);
	}
}
