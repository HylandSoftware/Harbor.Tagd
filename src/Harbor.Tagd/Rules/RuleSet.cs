using System.Collections.Generic;

namespace Harbor.Tagd.Rules
{
	public class RuleSet
	{
		public class GlobalIgnoreSettings
		{
			public string[] Projects { get; set; }
			public string[] Repos { get; set; }
			public string[] Tags { get; set; }
		}

		public GlobalIgnoreSettings IgnoreGlobally { get; set; }
		public Rule DefaultRule { get; set; }
		public List<Rule> Rules { get; set; }

		public RuleSet EnsureDefaults()
		{
			Rules = Rules ?? new List<Rule>();
			IgnoreGlobally = IgnoreGlobally ?? new GlobalIgnoreSettings();
			IgnoreGlobally.Projects = IgnoreGlobally.Projects ?? new string[0];
			IgnoreGlobally.Repos = IgnoreGlobally.Repos ?? new string[0];
			IgnoreGlobally.Tags = IgnoreGlobally.Tags ?? new string[0];

			return this;
		}
	}
}
