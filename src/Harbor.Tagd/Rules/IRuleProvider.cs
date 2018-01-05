using System.Collections.Generic;

namespace Harbor.Tagd.Rules
{
	public interface IRuleProvider
	{
		RuleSet Load();
	}
}
