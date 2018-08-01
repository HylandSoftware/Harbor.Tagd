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
				Assert.Equal("The rule provider did not provide any rules", ex.Message);
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
				Assert.Equal("No default rule was provided", ex.Message);
			}
		}
	}
}
