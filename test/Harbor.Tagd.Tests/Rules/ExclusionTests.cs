using AutoFixture.Xunit2;
using Harbormaster.Models;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace Harbor.Tagd.Tests.Rules
{
	public class ExclusionTests : DefaultRuleTest
	{
		public ExclusionTests()
		{
			_ruleSet.DefaultRule.Keep = 0;
		}

		[Theory, AutoData]
		public async Task ExcludesGloballyIgnoredProject(Project p)
		{
			_ruleSet.IgnoreGlobally.Projects = new[] { p.Name };

			Harbor.Setup(h => h.GetAllProjects(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>())).ReturnsAsync(new[] { p });

			await _sut.Process();

			Serilog.Verify(l => l.Verbose("Skipping project {@project}", p.Name), Times.Once);
			Harbor.Verify(h => h.GetRepositories(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
		}

		[Theory, AutoData]
		public async Task ExcludesGloballyIgnoredRepos(Project p, Repository r)
		{
			_ruleSet.IgnoreGlobally.Repos = new[] { r.Name };

			Harbor.Setup(h => h.GetAllProjects(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>())).ReturnsAsync(new[] { p });
			Harbor.Setup(h => h.GetRepositories(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new[] { r });

			await _sut.Process();

			Serilog.Verify(l => l.Verbose("Skipping repository {@repository}", r), Times.Once);
			Harbor.Verify(h => h.GetTags(It.IsAny<string>()), Times.Never);
		}

		[Theory, AutoData]
		public async Task ExcludesGloballyIgnoredTags(Project p, Repository r, Tag t)
		{
			_ruleSet.IgnoreGlobally.Tags = new[] { t.Name };

			Harbor.Setup(h => h.GetAllProjects(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>())).ReturnsAsync(new[] { p });
			Harbor.Setup(h => h.GetRepositories(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(new[] { r });
			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(new[] { t });

			await _sut.Process();

			Serilog.Verify(l => l.Information("Tag {repo}:{name} skipped due to global ignore rules", t.Repository, t.Name), Times.Once);
			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		}
	}
}
