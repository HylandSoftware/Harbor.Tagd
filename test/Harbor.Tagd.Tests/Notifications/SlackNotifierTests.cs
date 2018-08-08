using AutoFixture;
using AutoFixture.Xunit2;
using Flurl.Http.Testing;
using Harbor.Tagd.Args;
using Harbor.Tagd.Notifications;
using Harbor.Tagd.Tests.Extensions;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Harbor.Tagd.Tests.Notifications
{
	public class SlackNotifierTests : IDisposable
	{
		private const string MOCK_HOOK = "https://slack.mydomain.net/hook/abc123";

		private readonly Fixture _fixture;
		private readonly HttpTest _http;

		public SlackNotifierTests()
		{
			_fixture = new Fixture();
			_http = new HttpTest();
		}

		[Theory, AutoData]
		public async Task Notify_Fallback_IncludesProperties(ProcessResult result)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.Notify(result);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonPropertyMatching(b => b.attachments[0].fallback, $"Tag cleanup complete on {settings.Endpoint}: Removed {result.RemovedTags} tags, ignored {result.IgnoredTags} tags, {result.IgnoredRepos} repos, and {result.IgnoredProjects} projects$");
		}

		[Theory, AutoData]
		public async Task Notify_TitleContainsEndpoint(ProcessResult result)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.Notify(result);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonPropertyMatching(b => b.attachments[0].title, $"Tag cleanup complete on {settings.Endpoint}");
		}

		[Theory, AutoData]
		public async Task Notify_IncludesDryRun(ProcessResult result)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.With(f => f.Nondestructive, true)
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.Notify(result);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonPropertyMatching(b => b.attachments[0].fallback, "DRY RUN - Tag cleanup complete on.*")
				.WithJsonPropertyMatching(b => b.attachments[0].title, "DRY RUN - Tag cleanup complete on.*");
		}

		[Theory, AutoData]
		public async Task Notify_SkipsDryRun(ProcessResult result)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.With(f => f.Nondestructive, false)
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.Notify(result);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonPropertyMatching(b => b.attachments[0].fallback, "^Tag cleanup complete on.*")
				.WithJsonPropertyMatching(b => b.attachments[0].title, "^Tag cleanup complete on.*");
		}

		[Theory, AutoData]
		public async Task Notify_TitleLinkForcesHttpsIfMissingProto(ProcessResult result)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.With(f => f.Endpoint, "hcr.mydomain.net")
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.Notify(result);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonProperty<string>(b => b.attachments[0].title_link, "https://hcr.mydomain.net");
		}

		[Theory, AutoData]
		public async Task Notify_IncludesFields(ProcessResult result)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.Notify(result);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonProperty<string>(b => b.attachments[0].fields[0].title, "Removed Tags")
				.WithJsonProperty<int>(b => b.attachments[0].fields[0].value, result.RemovedTags)
				.WithJsonProperty<string>(b => b.attachments[0].fields[1].title, "Ignored Tags")
				.WithJsonProperty<int>(b => b.attachments[0].fields[1].value, result.IgnoredTags)
				.WithJsonProperty<string>(b => b.attachments[0].fields[2].title, "Ignored Repos")
				.WithJsonProperty<int>(b => b.attachments[0].fields[2].value, result.IgnoredRepos)
				.WithJsonProperty<string>(b => b.attachments[0].fields[3].title, "Ignored Projects")
				.WithJsonProperty<int>(b => b.attachments[0].fields[3].value, result.IgnoredProjects);
		}

		[Theory, AutoData]
		public async Task NotifyException_Fallback_IncludesMessage(Exception dummy)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.With(f => f.Nondestructive, false)
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.NotifyUnhandledException(dummy);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonProperty<string>(b => b.attachments[0].fallback, $"Unhandled Exception encountered during tag cleanup: {dummy.Message}");
		}

		[Theory, AutoData]
		public async Task NotifyException_IncludesDryRun(Exception dummy)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.With(f => f.Nondestructive, true)
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.NotifyUnhandledException(dummy);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonPropertyMatching(b => b.attachments[0].fallback, "DRY RUN - Unhandled Exception encountered during tag cleanup.*")
				.WithJsonPropertyMatching(b => b.attachments[0].title, "DRY RUN - Unhandled Exception encountered during tag cleanup.*");
		}

		[Theory, AutoData]
		public async Task NotifyException_SkipsDryRun(Exception dummy)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.With(f => f.Nondestructive, false)
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.NotifyUnhandledException(dummy);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonPropertyMatching(b => b.attachments[0].fallback, "^Unhandled Exception encountered during tag cleanup.*")
				.WithJsonPropertyMatching(b => b.attachments[0].title, "^Unhandled Exception encountered during tag cleanup.*");
		}

		[Theory, AutoData]
		public async Task NotifyException_TitleLinkForcesHttpsIfMissingProto(Exception dummy)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.With(f => f.Endpoint, "hcr.mydomain.net")
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.NotifyUnhandledException(dummy);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonProperty<string>(b => b.attachments[0].title_link, "https://hcr.mydomain.net");
		}

		[Theory, AutoData]
		public async Task NotifyException_PayloadIsStackTrace(Exception dummy)
		{
			var settings = _fixture.Build<ApplicationSettings>()
				.WithAutoProperties()
				.With(f => f.SlackWebhook, MOCK_HOOK)
				.Create();

			var _sut = new SlackResultNotifier(settings);

			await _sut.NotifyUnhandledException(dummy);

			_http.ShouldHaveCalled(MOCK_HOOK)
				.WithVerb(HttpMethod.Post)
				.WithJsonProperty<string>(b => b.attachments[0].text, $"```csharp\n{dummy.ToString()}\n```");
		}

		public void Dispose() => _http.Dispose();
	}
}
