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
	}
}
