using Serilog;
using System;
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

		public RuleSet Check()
		{
			if (DefaultRule == null)
			{
				throw new ArgumentException("No default rule was provided");
			}

			if ((Rules?.Count ?? 0) == 0)
			{
				Log.Warning("The rule provider did not return any explicit tag rules");
			}

			foreach (var rule in Rules)
			{
				Log.Verbose("Found rule {rule}", rule);
			}
			Log.Verbose("Using default rule {defaultRule}", DefaultRule);

			Log.Verbose("Ignoring the following projects: {projects}", IgnoreGlobally?.Projects);
			Log.Verbose("Ignoring the following repos: {repos}", IgnoreGlobally?.Repos);
			Log.Verbose("Ignoring the following tags: {tags}", IgnoreGlobally?.Tags);
			return this;
		}
	}
}
