using Harbor.Tagd.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Harbor.Tagd.Rules
{
	internal class ConfigServerRuleProvider : IRuleProvider
	{
		public static readonly string RULES_KEY = "rules";
		public static readonly string DEFAULT_RULE_KEY = "defaultRule";
		public static readonly string IGNORE_GLOBALLY_KEY = "ignoreGlobally";

		private readonly ConfigServerClientSettings _settings;
		private readonly IConfigurationRoot _cloudConfig;

		public ConfigServerRuleProvider(ConfigServerClientSettings settings, ILoggerFactory logger = null)
		{
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));

			_cloudConfig = new ConfigurationBuilder()
				.AddConfigServer(_settings, logger)
				.Build();
		}

		// TODO: If we can figure out how to tell the binder to map string -> regex we can just _cloudConfig.Bind(ruleSet)
		public RuleSet Load()
		{
			var defaultRule = _cloudConfig.GetSection(DEFAULT_RULE_KEY);
			var rules = _cloudConfig.GetSection(RULES_KEY);

			var catchAll = ".*".ToCompiledRegex();

			RuleSet.GlobalIgnoreSettings g = new RuleSet.GlobalIgnoreSettings();
			_cloudConfig.GetSection(IGNORE_GLOBALLY_KEY).Bind(g);

			return new RuleSet
			{
				DefaultRule = defaultRule.ToRule(),
				Rules = rules.GetChildren().Select(r => r.ToRule()).ToList(),
				IgnoreGlobally = g
			};
		}
	}

	internal static class IConfigurationSectionExtensions
	{
		public static Rule ToRule(this IConfigurationSection section) => new Rule
		{
			Project = section.GetValue<string>("project")?.ToCompiledRegex() ?? new Regex(".*"),
			Repo = section.GetValue<string>("repo")?.ToCompiledRegex() ?? new Regex(".*"),
			Tag = section.GetValue<string>("tag")?.ToCompiledRegex() ?? new Regex(".*"),
			Ignore = section.GetValue<string[]>("ignore"),
			Keep = section.GetValue<int>("keep").EnsurePositive()
		};
	}
}
