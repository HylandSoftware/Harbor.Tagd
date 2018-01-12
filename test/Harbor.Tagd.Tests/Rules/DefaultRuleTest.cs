using Harbor.Tagd.Rules;
using System.Text.RegularExpressions;

namespace Harbor.Tagd.Tests.Rules
{
	public class DefaultRuleTest : TagEngineTest
	{
		protected readonly RuleSet _ruleSet;

		public DefaultRuleTest()
		{
			_ruleSet = new RuleSet
			{
				DefaultRule = new Rule
				{
					Project = new Regex(".*"),
					Repo = new Regex(".*"),
					Tag = new Regex(".*"),
					Ignore = new string[0],
					Keep = 5
				}
			}.EnsureDefaults();

			Rules.Setup(r => r.Load()).Returns(() => _ruleSet);
		}
	}
}
