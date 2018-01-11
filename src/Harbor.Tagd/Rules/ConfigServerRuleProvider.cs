using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Extensions.Configuration.ConfigServer;

namespace Harbor.Tagd.Rules
{
	internal class ConfigServerRuleProvider : DotNetConfigurationRuleProvider
	{
		public ConfigServerRuleProvider(ConfigServerClientSettings settings, ILoggerFactory logger = null) : base
		(
			new ConfigurationBuilder()
				.AddConfigServer(settings, logger)
				.Build()
		)
		{
		}
	}
}
