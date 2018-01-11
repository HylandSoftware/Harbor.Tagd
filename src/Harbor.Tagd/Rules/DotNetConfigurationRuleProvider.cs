using Harbor.Tagd.Extensions;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Harbor.Tagd.Rules
{
	internal abstract class DotNetConfigurationRuleProvider : IRuleProvider
	{
		public static readonly string RULES_KEY = "rules";
		public static readonly string DEFAULT_RULE_KEY = "defaultRule";
		public static readonly string IGNORE_GLOBALLY_KEY = "ignoreGlobally";


		private readonly IConfigurationRoot _root;

		public DotNetConfigurationRuleProvider(IConfigurationRoot root) =>
			_root = root ?? throw new ArgumentNullException(nameof(root));

		// TODO: If we can figure out how to tell the binder to map string -> regex we can just _cloudConfig.Bind(ruleSet)
		public RuleSet Load()
		{
			var defaultRule = _root.GetSection(DEFAULT_RULE_KEY);
			var rules = _root.GetSection(RULES_KEY);

			var catchAll = ".*".ToCompiledRegex();

			RuleSet.GlobalIgnoreSettings g = new RuleSet.GlobalIgnoreSettings();
			_root.GetSection(IGNORE_GLOBALLY_KEY).Bind(g);

			return new RuleSet
			{
				DefaultRule = defaultRule.ToRule(),
				Rules = rules.GetChildren().Select(r => r.ToRule()).ToList(),
				IgnoreGlobally = g
			}.EnsureDefaults();
		}
	}

	internal static class IConfigurationSectionExtensions
	{
		private static readonly Regex _catchAll = new Regex(".*", RegexOptions.Compiled);

		public static Rule ToRule(this IConfigurationSection section) => new Rule
		{
			Project = section.GetValue<string>("project")?.ToCompiledRegex() ?? _catchAll,
			Repo = section.GetValue<string>("repo")?.ToCompiledRegex() ?? _catchAll,
			Tag = section.GetValue<string>("tag")?.ToCompiledRegex() ?? _catchAll,
			Ignore = section.GetValue<string[]>("ignore"),
			Keep = section.GetValue<int>("keep").EnsurePositive()
		};
	}
}
