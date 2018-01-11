using AutoFixture.Xunit2;
using Harbor.Tagd.Rules;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Harbor.Tagd.Tests
{
	public class RuleResolutionTests : TagEngineTest
	{
		public RuleResolutionTests()
		{
			Settings.DumpRules = true;
		}

		[Fact]
		public async Task ThrowsForNullRuleSet()
		{
			Rules.Setup(r => r.Load()).Returns<RuleSet>(null);

			try
			{
				await _sut.Process();
				Assert.True(false, "Expected an exception to be thrown");
			}
			catch(Exception ex)
			{
				Assert.Equal("The rule provider did not return a default rule", ex.Message);
			}
		}

		[Theory, AutoData]
		public async Task ThrowsForNullDefaultRule(List<Rule> tagRules)
		{
			Rules.Setup(r => r.Load()).Returns(new RuleSet { Rules = tagRules, DefaultRule = null });

			try
			{
				await _sut.Process();
				Assert.True(false, "Expected an exception to be thrown");
			}
			catch (Exception ex)
			{
				Assert.Equal("The rule provider did not return a default rule", ex.Message);
			}
		}

		[Theory, AutoData]
		public async Task LogsDiscoveredRules(RuleSet set)
		{
			Rules.Setup(r => r.Load()).Returns(set);

			await _sut.Process();

			foreach(var tagRule in set.Rules)
			{
				Serilog.Verify(s => s.Verbose("Found rule {rule}", tagRule), Times.Once);
			}

			Serilog.Verify(s => s.Verbose("Using default rule {defaultRule}", set.DefaultRule), Times.Once);
			Serilog.Verify(s => s.Verbose("Ignoring the following projects: {projects}", set.IgnoreGlobally.Projects), Times.Once);
			Serilog.Verify(s => s.Verbose("Ignoring the following repos: {repos}", set.IgnoreGlobally.Repos), Times.Once);
			Serilog.Verify(s => s.Verbose("Ignoring the following tags: {tags}", set.IgnoreGlobally.Tags), Times.Once);
		}

		[Theory, AutoData]
		public async Task OnlyDumpsRulesInDumpMode(RuleSet set)
		{
			Rules.Setup(r => r.Load()).Returns(set);

			await _sut.Process();

			Harbor.Verify(h => h.Login(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
		}
	}
}
