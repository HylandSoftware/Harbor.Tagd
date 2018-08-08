using Harbor.Tagd.Rules;
using System;
using System.IO;
using Xunit;
using YamlDotNet.Core;

namespace Harbor.Tagd.Tests.Rules
{
	public class FilesystemRuleProviderTests : IDisposable
	{
		private readonly string TestFile;
		private readonly FilesystemRuleProvider _sut;

		public FilesystemRuleProviderTests()
		{
			TestFile = Path.GetTempFileName();
			_sut = new FilesystemRuleProvider(TestFile);
		}

		[Fact]
		public void Valid()
		{
			File.WriteAllText(TestFile, @"
ignoreGlobally:
  projects: ['foo']
  repos: ['bar']
  tags: ['latest']
defaultRule:
  project: 'a.*'
  repo: 'b.*'
  tag: 'c.*'
  ignore: ['latest']
  keep: 10
rules:
- project: 'foo'
  repo: 'bar-api'
  tag: '^\d+\.\d+\.\d+(?:-beta(?:\.\d+)?)?$'
  ignore: ['master', 'develop']
  keep: 123456789
");

			var rules = _sut.Load();

			Assert.NotNull(rules);

			Assert.Single(rules.IgnoreGlobally.Projects);
			Assert.Contains("foo", rules.IgnoreGlobally.Projects);
			Assert.Single(rules.IgnoreGlobally.Repos);
			Assert.Contains("bar", rules.IgnoreGlobally.Repos);
			Assert.Single(rules.IgnoreGlobally.Tags);
			Assert.Contains("latest", rules.IgnoreGlobally.Tags);

			Assert.Equal(10, rules.DefaultRule.Keep);
			Assert.Equal("a.*", rules.DefaultRule.Project.ToString());
			Assert.Equal("b.*", rules.DefaultRule.Repo.ToString());
			Assert.Equal("c.*", rules.DefaultRule.Tag.ToString());
			Assert.Single(rules.DefaultRule.Ignore);
			Assert.Contains("latest", rules.DefaultRule.Ignore);

			Assert.Single(rules.Rules);
			Assert.Equal("foo", rules.Rules[0].Project.ToString());
			Assert.Equal("bar-api", rules.Rules[0].Repo.ToString());
			Assert.Equal(@"^\d+\.\d+\.\d+(?:-beta(?:\.\d+)?)?$", rules.Rules[0].Tag.ToString());
			Assert.Equal(2, rules.Rules[0].Ignore.Length);
			Assert.Contains("master", rules.Rules[0].Ignore);
			Assert.Contains("develop", rules.Rules[0].Ignore);
			Assert.Equal(123456789, rules.Rules[0].Keep);
		}

		[Fact]
		public void ThrowsForNoFile()
		{
			File.Delete(TestFile);

			Assert.Throws<FileNotFoundException>(() => _sut.Load());
		}

		[Fact]
		public void ThrowsForBadYaml()
		{
			File.WriteAllText(TestFile, "thisisnotyaml[");

			Assert.Throws<YamlException>(() => _sut.Load());
		}


		public void Dispose()
		{
			try
			{
				File.Delete(TestFile);
			}
			catch { }
		}
	}
}
