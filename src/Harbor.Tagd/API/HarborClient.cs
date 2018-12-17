using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Harbor.Tagd.API.Models;
using Harbor.Tagd.API.Support;
using SemVer;
using Serilog;
using Version = SemVer.Version;

namespace Harbor.Tagd.API
{
	internal class HarborClient : IHarborClient
	{
		private const string SESSION_COOKIE_KEY = "beegosessionID";
		private const string NEW_SESSION_COOKIE_KEY = "sid";
		private readonly Range _loginRefactorVersion = new Range(">=1.7.0");

		private string sessionTokenName;
		private Cookie sessionToken;

		public string SessionToken { get { return sessionToken?.Value; } }
		public string Endpoint { get; }

		public HarborClient(string endpoint) => Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

		private IFlurlRequest CreateCall()
		{
			var request = Endpoint.AppendPathSegment("api").EnableCookies();

			if (string.IsNullOrEmpty(SessionToken))
			{
				throw new InvalidOperationException("This API Call requires a session. Call Login(...) first");
			}

			return request;
		}
		
		private async Task<Version> ProbeVersion()
		{
			var info = await Endpoint.AppendPathSegments("api", "systeminfo").GetJsonAsync<SystemInfo>();
			return new Version(info.Version.Substring(1, info.Version.IndexOf("-")-1));
		}

		public async Task Login(string user, string password)
		{
			Url path;

			var v = await ProbeVersion();
			if (_loginRefactorVersion.IsSatisfied(v))
			{
				Log.Debug("Newer version of harbor found, using 1.7.0+ login route");
				path = Endpoint.AppendPathSegments("c", "login");
				sessionTokenName = NEW_SESSION_COOKIE_KEY;
			}
			else
			{
				Log.Debug("Older version of harbor found, using pre-1.7.0 login route");
				path = Endpoint.AppendPathSegment("login");
				sessionTokenName = SESSION_COOKIE_KEY;
			}

			var client = path.AllowAnyHttpStatus().EnableCookies();
			var response = await client.PostUrlEncodedAsync(new { principal = user, password = password });
			
			try
			{
				response.EnsureSuccessStatusCode();
			}
			catch (Exception)
			{
				if (response.StatusCode == HttpStatusCode.Unauthorized)
				{
					throw new ArgumentException("Invalid username or password");
				}
				else
				{
					throw;
				}
			}

			sessionToken = client.Cookies[sessionTokenName] ?? throw new Exception("Failed to parse session token");
		}

		public async Task Logout()
		{
			if (sessionToken == null) throw new InvalidOperationException("Not logged in");
			await Endpoint.AppendPathSegment("log_out").GetAsync();
			sessionToken = null;
		}

		public async Task<IEnumerable<Project>> GetAllProjects() => await CreateCall()
			.AppendPathSegment("projects")
			.GetJsonAsync<IEnumerable<Project>>();

		public async Task<IEnumerable<Repository>> GetRepositories(int project) => await CreateCall()
			.AppendPathSegment("repositories")
			.SetQueryParam("project_id", project)
			.GetJsonAsync<IEnumerable<Repository>>();

		public async Task<IEnumerable<Tag>> GetTags(string repo) =>
			(
				await CreateCall()
				  .AppendPathSegment("repositories")
				  .AppendPathSegments(repo.ValidateAndSplit())
				  .AppendPathSegment("tags")
				  .GetJsonAsync<IEnumerable<Tag>>()
			).Modify(t => t.Repository = repo);

		public async Task DeleteTag(string repo, string tag) =>
			await CreateCall()
				.AppendPathSegment("repositories")
				.AppendPathSegments(repo.ValidateAndSplit())
				.AppendPathSegments("tags", tag)
				.DeleteAsync();
	}
}
