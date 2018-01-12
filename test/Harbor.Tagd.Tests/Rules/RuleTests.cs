using AutoFixture;
using AutoFixture.Xunit2;
using Harbor.Tagd.Rules;
using Harbormaster.Models;
using Moq;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;

namespace Harbor.Tagd.Tests.Rules
{
	public class RuleTests : DefaultRuleTest
	{
		private readonly Project _project;
		private readonly Repository _repository;

		public RuleTests()
		{
			Settings.ReportOnly = false;

			_project = _fixture.Create<Project>();
			_repository = _fixture.Create<Repository>();

			Harbor.Setup(h => h.GetAllProjects(It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string>())).ReturnsAsync(() => new[] { _project });
			Harbor.Setup(h => h.GetRepositories(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(() => new[] { _repository });
		}

		[Theory, AutoData]
		public async Task RuleExcludesTag(Tag t)
		{
			_fixture.Inject(new Regex(".*"));
			_ruleSet.Rules.Add(_fixture.Build<Rule>().WithAutoProperties().With(r => r.Ignore, new[] { t.Name }).Create());

			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(new[] { t });

			await _sut.Process();

			Serilog.Verify(l => l.Information("Tag {repo}:{name} skipped because it was found in an ignore list that applies to {repo}", t.Repository, t.Name, _repository.Name), Times.Once);
			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		}

		[Fact]
		public async Task KeepsSpecifiedNumberOfTagsViaDefault()
		{
			var tags = _fixture.CreateMany<Tag>(_ruleSet.DefaultRule.Keep + 5);
			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(tags);

			await _sut.Process();

			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(5));
		}

		[Fact]
		public async Task KeptByRule()
		{
			_fixture.Inject(new Regex(".*"));
			_ruleSet.Rules.Add(_fixture.Build<Rule>().WithAutoProperties().With(r => r.Keep, 5).Without(r => r.Ignore).Create());
			_ruleSet.DefaultRule.Tag = new Regex("^$");

			var tags = _fixture.CreateMany<Tag>(10);
			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(tags);

			await _sut.Process();

			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(5));
		}

		[Fact]
		public async Task DefaultRuleProcessesRemainingTags()
		{
			_fixture.Inject(new Regex("^((?!foo).)*$"));
			_ruleSet.Rules.Add(_fixture.Build<Rule>().WithAutoProperties().With(r => r.Keep, 5).Without(r => r.Ignore).Create());
			_ruleSet.DefaultRule.Tag = new Regex("^foo.*$");
			_ruleSet.DefaultRule.Keep = 3;

			var tags = _fixture.CreateMany<Tag>(10).ToList();

			foreach(var tag in _fixture.CreateMany<Tag>(10))
			{
				tag.Name = $"foo{tag.Name}";
				tags.Add(tag);
			}

			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(tags);

			await _sut.Process();

			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(12));
		}

		[Fact]
		public async Task UnmatchedTagsAreKeptImplicitly()
		{
			_ruleSet.DefaultRule.Tag = new Regex("^$");

			var tags = _fixture.CreateMany<Tag>(10);
			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(tags);

			await _sut.Process();

			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
			Serilog.Verify(l => l.Warning("The default rule did not match all remaining tags for {@repo}. {count} remaining tags will be kept", _repository, 10), Times.Once);
		}

		[Fact]
		public async Task KeepsTagIgnoredByDefaultRule()
		{
			_ruleSet.DefaultRule.Ignore = new[] { "latest" };

			var tags = _fixture.CreateMany<Tag>(10).Append(_fixture.Build<Tag>().WithAutoProperties().With(t => t.Name, "latest").Create());

			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(tags);

			await _sut.Process();

			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(5));
			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), "latest"), Times.Never);
		}

		[Fact]
		public async Task TagsThatHaveTheSameDigestAreKept()
		{
			_ruleSet.IgnoreGlobally.Tags = new[] { "latest" };

			var tags = _fixture.CreateMany<Tag>(10).ToList();

			var digest = _fixture.Create<string>();
			var latest = _fixture.Build<Tag>().WithAutoProperties().With(t => t.Digest, digest).With(t => t.Name, "latest").Create();
			var dup = _fixture.Build<Tag>().WithAutoProperties().With(t => t.Digest, digest).With(t => t.Name, "dup").With(t => t.CreatedAt, DateTime.MinValue).Create();

			Harbor.Setup(h => h.GetTags(It.IsAny<string>())).ReturnsAsync(tags.Union(new[] { latest, dup}));

			await _sut.Process();

			Harbor.Verify(h => h.DeleteTag(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(5));
			Harbor.Verify(h => h.DeleteTag(latest.Repository, latest.Name), Times.Never);
			Harbor.Verify(h => h.DeleteTag(dup.Repository, dup.Name), Times.Never);
		}
	}
}
