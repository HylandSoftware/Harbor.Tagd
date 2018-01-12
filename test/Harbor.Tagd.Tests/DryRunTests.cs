using AutoFixture;
using Harbor.Tagd.Rules;
using Harbormaster.Models;
using Moq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Harbor.Tagd.Tests
{
	public class DryRunTests : TagEngineTest
	{
		[Fact]
		public async Task DoesNotDeleteOnDryRun()
		{
			Settings.Nondestructive = true;

			var tags = _fixture.CreateMany<Tag>(10);
			var project = _fixture.Create<Project>();
			var repo = _fixture.Create<Repository>();

			Rules.Setup(r => r.Load()).Returns(new RuleSet
			{
				DefaultRule = new Rule
				{
					Project = new Regex(".*"),
					Repo = new Regex(".*"),
					Tag = new Regex(".*"),
					Keep = 3
				}
			}.EnsureDefaults());

			Harbor.Setup(h => h.GetAllProjects(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>())).ReturnsAsync(new[] { project });
			Harbor.Setup(h => h.GetRepositories(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new[] { repo });
			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(tags);

			await _sut.Process();

			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		}
	}
}
