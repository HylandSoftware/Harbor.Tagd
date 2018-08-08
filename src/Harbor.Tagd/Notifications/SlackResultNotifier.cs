using Flurl.Http;
using Harbor.Tagd.Args;
using System;
using System.Threading.Tasks;

namespace Harbor.Tagd.Notifications
{
	internal class SlackResultNotifier : IResultNotifier
	{
		private readonly ApplicationSettings _settings;



		public SlackResultNotifier(ApplicationSettings settings) => _settings = settings ?? throw new ArgumentNullException(nameof(settings));

		public async Task Notify(ProcessResult result) => await SendPayload(new []
		{
			new
			{
				fallback = $"{(_settings.Nondestructive ? "DRY RUN - " : "")}Tag cleanup complete on {_settings.Endpoint}: Removed {result.RemovedTags} tags, ignored {result.IgnoredTags} tags, {result.IgnoredRepos} repos, and {result.IgnoredProjects} projects",
				title = $"{(_settings.Nondestructive ? "DRY RUN - " : "")}Tag cleanup complete on {_settings.Endpoint}",
				title_link = _settings.Endpoint.ToLower().StartsWith("http") ? _settings.Endpoint : $"https://{_settings.Endpoint}",
				fields = new[] {
					new { title = "Removed Tags", value = result.RemovedTags.ToString(), @short = true },
					new { title = "Ignored Tags", value = result.IgnoredTags.ToString(), @short = true },
					new { title = "Ignored Repos", value = result.IgnoredRepos.ToString(), @short = true },
					new { title = "Ignored Projects", value = result.IgnoredProjects.ToString(), @short = true },
				},
				ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			}
		});

		public async Task NotifyUnhandledException(Exception ex) => await SendPayload(new []
		{
			new
			{
				fallback = $"{(_settings.Nondestructive ? "DRY RUN - " : "")}Unhandled Exception encountered during tag cleanup: {ex.Message}",
				title = $"{(_settings.Nondestructive ? "DRY RUN - " : "")}Unhandled Exception encountered during tag cleanup",
				title_link = _settings.Endpoint.ToLower().StartsWith("http") ? _settings.Endpoint : $"https://{_settings.Endpoint}",
				text = $"```csharp\n{ex.ToString()}\n```",
				color = "danger",
				ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			}
		});

		private async Task SendPayload(object[] attachments) => await _settings.SlackWebhook.PostJsonAsync(new
		{
			attachments,
			username = "tagd",
			icon_url = "https://secure.gravatar.com/avatar/26da7b36ff8bb5db4211400358dc7c4e.jpg?s=512&r=g&d=mm"
		});
	}
}
