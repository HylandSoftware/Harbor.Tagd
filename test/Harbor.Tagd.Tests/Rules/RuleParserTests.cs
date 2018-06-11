using Harbor.Tagd.Rules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System;
using Xunit;

namespace Harbor.Tagd.Tests.Rules
{
	public class RuleParserTests
	{
		internal class TestRuleProvider : DotNetConfigurationRuleProvider
		{
			public TestRuleProvider(IConfigurationRoot root) : base(root)
			{
			}
		}

		private static IConfigurationRoot LoadConfiguration(string resource) =>
			new ConfigurationBuilder()
				.AddJsonFile(new EmbeddedFileProvider(typeof(TestRuleProvider).Assembly), resource, false, false)
				.Build();

		[Fact]
		public void ThrowsForNegativeToKeep()
		{
			var sut = new TestRuleProvider(LoadConfiguration("Rules/TestRules/negativeToKeep.json"));

			Assert.Throws<ArgumentException>(() => sut.Load());
		}

		[Fact]
		public void UsesCatchAllForNullRegex()
		{
			var sut = new TestRuleProvider(LoadConfiguration("Rules/TestRules/defaults.json"));

			var rules = sut.Load();

			Assert.Equal(".*", rules.DefaultRule.Project.ToString());
			Assert.Equal(".*", rules.DefaultRule.Repo.ToString());
			Assert.Equal(".*", rules.DefaultRule.Tag.ToString());
		}

		[Fact]
		public void ParsesGlobalIgnoreList()
		{
			var sut = new TestRuleProvider(LoadConfiguration("Rules/TestRules/valid.json"));

			var rules = sut.Load();

			Assert.Equal(3, rules.IgnoreGlobally.Projects.Length);
			Assert.Contains("p1", rules.IgnoreGlobally.Projects);
			Assert.Contains("p2", rules.IgnoreGlobally.Projects);
			Assert.Contains("p3", rules.IgnoreGlobally.Projects);

			Assert.Equal(3, rules.IgnoreGlobally.Repos.Length);
			Assert.Contains("r1", rules.IgnoreGlobally.Repos);
			Assert.Contains("r2", rules.IgnoreGlobally.Repos);
			Assert.Contains("r3", rules.IgnoreGlobally.Repos);

			Assert.Equal(3, rules.IgnoreGlobally.Tags.Length);
			Assert.Contains("gt1", rules.IgnoreGlobally.Tags);
			Assert.Contains("gt2", rules.IgnoreGlobally.Tags);
			Assert.Contains("gt3", rules.IgnoreGlobally.Tags);
		}

		[Fact]
		public void ParsesDefaultRule()
		{
			var sut = new TestRuleProvider(LoadConfiguration("Rules/TestRules/valid.json"));

			var rules = sut.Load();

			Assert.Equal("^foo$", rules.DefaultRule.Project.ToString());
			Assert.Equal("^bar$", rules.DefaultRule.Repo.ToString());
			Assert.Equal("^baz$", rules.DefaultRule.Tag.ToString());

			Assert.Equal(3, rules.DefaultRule.Ignore.Length);
			Assert.Contains("a", rules.DefaultRule.Ignore);
			Assert.Contains("b", rules.DefaultRule.Ignore);
			Assert.Contains("c", rules.DefaultRule.Ignore);

			Assert.Equal(123, rules.DefaultRule.Keep);
		}

		[Fact]
		public void ParsesIgnoreList()
		{
			var sut = new TestRuleProvider(LoadConfiguration("Rules/TestRules/valid.json"));

			var rules = sut.Load();

			Assert.Single(rules.Rules);

			var r = rules.Rules[0];
			Assert.Single(r.Ignore);
			Assert.Equal("develop", r.Ignore[0]);
		}
	}
}
