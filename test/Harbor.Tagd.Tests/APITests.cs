using AutoFixture.Xunit2;
using Flurl.Http.Testing;
using Harbor.Tagd.API;
using Harbor.Tagd.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Harbor.Tagd.Tests
{
	public class APITests : IDisposable
	{
		private readonly HttpTest _http;
		private readonly HarborClient client;

		public APITests()
		{
			_http = new HttpTest();
			client = new HarborClient("http://localhost:5555/");
		}

		[Theory, AutoData]
		public async Task CanLogin(string token, string user, string password)
		{
			ConfigureAuthToken(token);

			await client.Login(user, password);

			Assert.Equal(token, client.SessionToken);
			_http.ShouldHaveCalled("*/login").WithVerb(HttpMethod.Post).WithRequestBody($"principal={user}&password={password}").Times(1);
		}

		[Theory, AutoData]
		public async Task SendsSessionCookieWhenLoggedIn(string token, string user, string password, IEnumerable<Project> projects)
		{
			ConfigureAuthToken(token);
			_http.RespondWithJson(projects);

			await client.Login(user, password);
			await client.GetAllProjects();

			_http.ShouldHaveCalled("*").WithHeader("Cookie", $"*beegosessionID={token}*");
		}

		[Theory, AutoData]
		public async Task ThrowsForInvalidCredentials(string user, string password)
		{
			_http.RespondWith(status: 401);

			try
			{
				await client.Login(user, password);
				Assert.True(false, "Expected exception to be thrown");
			}
			catch (Exception ex)
			{
				Assert.IsAssignableFrom<ArgumentException>(ex);
				Assert.Equal("Invalid username or password", ex.Message);
				_http.ShouldHaveCalled("*/login").WithVerb(HttpMethod.Post).WithRequestBody($"principal={user}&password={password}").Times(1);
			}
		}

		[Theory, AutoData]
		public async Task ThrowsForUnknownError(string user, string password)
		{
			_http.RespondWith(status: 500);

			try
			{
				await client.Login(user, password);
				Assert.True(false, "Expected exception to be thrown");
			}
			catch (Exception ex)
			{
				Assert.IsAssignableFrom<HttpRequestException>(ex);
			}
		}

		[Theory, AutoData]
		public async Task CanLogout(string token, string user, string password)
		{
			ConfigureAuthToken(token);

			await client.Login(user, password);
			await client.Logout();

			Assert.Null(client.SessionToken);
			_http.ShouldHaveCalled("*/log_out").WithVerb(HttpMethod.Get).Times(1);
		}

		[Fact]
		public async Task Logout_ThrowsWithNoSession()
		{
			try
			{
				await client.Logout();
				Assert.True(false, "Expected exception to be thrown");
			}
			catch (Exception ex)
			{
				Assert.IsAssignableFrom<InvalidOperationException>(ex);
				Assert.Equal("Not logged in", ex.Message);
				_http.ShouldNotHaveCalled("*");
			}
		}

		[Theory, AutoData]
		public async Task GetAllProjects_Valid(List<Project> projects)
		{
			await MockSession();

			_http.RespondWithJson(projects);

			var result = (await client.GetAllProjects()).ToList();

			Assert.Equal(projects.Count, result.Count);
			Assert.All(result, p => projects.Contains(p));
			_http.ShouldHaveCalled("*/api/projects").WithoutQueryParams("name", "public", "owner").Times(1);
		}

		[Theory, AutoData]
		public async Task GetRepositories(int project, List<Repository> repos)
		{
			await MockSession();

			_http.RespondWithJson(repos);

			var result = (await client.GetRepositories(project)).ToList();

			Assert.Equal(repos.Count, result.Count);
			Assert.All(result, r => repos.Contains(r));
			_http.ShouldHaveCalled("*/api/repositories").WithQueryParamValue("project_id", project).WithoutQueryParam("q").WithVerb(HttpMethod.Get).Times(1);
		}

		[Theory, AutoData]
		public async Task GetAllTags(string r, string p, List<Tag> tags)
		{
			await MockSession();

			_http.RespondWithJson(tags);

			foreach (var t in tags)
			{
				t.Repository = $"{r}/{p}";
			}

			var result = (await client.GetTags($"{r}/{p}")).ToList();

			Assert.Equal(tags.Count, result.Count);
			Assert.True(result.All(t => tags.Contains(t)));
			_http.ShouldHaveCalled($"*/repositories/{r}/{p}/tags").WithVerb(HttpMethod.Get).Times(1);
		}

		[Theory, AutoData]
		public async Task GetAllTags_ThrowsForMalformedRepo(string repo)
		{
			await MockSession();

			try
			{
				await client.GetTags(repo);
				Assert.True(false, "Expected an exception to be thrown");
			}
			catch (Exception ex)
			{
				Assert.IsAssignableFrom<ArgumentException>(ex);
				Assert.Matches("Illegal Repository Path.*", ex.Message);
				_http.ShouldNotHaveCalled("*api/repositories*");
			}
		}

		[Theory, AutoData]
		public async Task DeleteTag(string r, string p, string tag)
		{
			await MockSession();

			await client.DeleteTag($"{r}/{p}", tag);
			_http.ShouldHaveCalled($"*/api/repositories/{r}/{p}/tags/{tag}").WithVerb(HttpMethod.Delete).Times(1);
		}

		[Theory, AutoData]
		public async Task DeleteTag_ThrowsForMalformedRepo(string repo, string tag)
		{
			await MockSession();

			try
			{
				await client.DeleteTag(repo, tag);
				Assert.True(false, "Expected an exception to be thrown");
			}
			catch (Exception ex)
			{
				Assert.IsAssignableFrom<ArgumentException>(ex);
				Assert.Matches("Illegal Repository Path.*", ex.Message);
				_http.ShouldNotHaveCalled("*/api/repositories*");
			}
		}

		[Theory, AutoData]
		public async Task ThrowsForNotLoggedIn(string repo, string tag) =>
			await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.DeleteTag(repo, tag));

		protected async Task MockSession()
		{
			ConfigureAuthToken("mockToken");
			await client.Login("mockUser", "mockPassword");
		}

		protected void ConfigureAuthToken(string token) => _http.RespondWith("", cookies: new { beegosessionID = token });

		public void Dispose() => _http.Dispose();
	}
}
