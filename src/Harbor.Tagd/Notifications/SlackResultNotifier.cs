using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Harbor.Tagd.Notifications
{
	internal class SlackResultNotifier : IResultNotifier
	{
		private readonly HarborSettings _settings;

		public SlackResultNotifier(HarborSettings settings) => _settings = settings ?? throw new ArgumentNullException(nameof(settings));

		public async Task Notify(ProcessResult result)
		{
			using (var client = new HttpClient())
			{
				await client.PostAsync(_settings.SlackWebhook, new StringContent(JsonConvert.SerializeObject(new
				{
					attachments = new []
					{
						new
						{
							fallback = $"{(_settings.ReportOnly ? "DRY RUN - " : "")}Tag cleanup complete on {_settings.Endpoint}: Removed {result.RemovedTags} tags, ignored {result.IgnoredTags} tags, {result.IgnoredRepos} repos, and {result.IgnoredProjects} projects",
							title = $"{(_settings.ReportOnly ? "DRY RUN - " : "")}Tag cleanup complete on {_settings.Endpoint}",
							title_link = _settings.Endpoint.ToLower().StartsWith("http") ? _settings.Endpoint : $"https://{_settings.Endpoint}",
							fields = new[] {
								new { title = "Removed Tags", value = result.RemovedTags.ToString(), @short = true },
								new { title = "Ignored Tags", value = result.IgnoredTags.ToString(), @short = true },
								new { title = "Ignored Repos", value = result.IgnoredRepos.ToString(), @short = true },
								new { title = "Ignored Projects", value = result.IgnoredProjects.ToString(), @short = true },
							},
							ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
						}
					},
					username = "tagd",
					icon_url = "https://secure.gravatar.com/avatar/26da7b36ff8bb5db4211400358dc7c4e.jpg?s=512&r=g&d=mm"
				}), Encoding.UTF8, "application/json"));
			}
		}
	}
}
