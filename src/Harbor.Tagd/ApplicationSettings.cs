using clipr;

namespace Harbor.Tagd
{
	[ApplicationInfo(Description = "Tag Cleanup daemon for VMware Harbor Registries", Name = "tagd")]
	public class ApplicationSettings
	{
		private const string CONFIG_METHOD = "config-method";

		[NamedArgument("endpoint", Description = "The harbor registry to connect to", Required = true)]
		public string Endpoint { get; set; }
		[NamedArgument('u', "user", Description = "The user to connect to harbor as", Required = true)]
		public string Username { get; set; }
		[NamedArgument('p', "password", Description = "The password for the user connecting to harbor", Required = true)]
		public string Password { get; set; }
		[NamedArgument('v', "verbosity", Description = "How verbose should logging output be")]
		public string Verbosity { get; set; } = "info";

		[NamedArgument("config-file", Description = "The config file to parse")]
		[MutuallyExclusiveGroup(CONFIG_METHOD, Required = true)]
		public string ConfigFile { get; set; }
		[NamedArgument("config-server", Description = "The springboot config server to get configuration from")]
		[MutuallyExclusiveGroup(CONFIG_METHOD)]
		public string ConfigServer { get; set; } = "http://localhost:8888";
		[NamedArgument("config-user", Description = "The user to login to the springboot config server as")]
		public string ConfigUser { get; set; }
		[NamedArgument("config-password", Description = "The password for the springboot config server user")]
		public string ConfigPassword { get; set; }

		[NamedArgument("destructive", Description = "Actually delete tags instead of generating a report", Action = ParseAction.StoreFalse)]
		public bool Nondestructive { get; set; } = true;

		[NamedArgument("dump-rules", Description = "Print the rules that would be used and exit", Action = ParseAction.StoreTrue)]
		public bool DumpRules { get; set; } = false;

		[NamedArgument("notify-slack", Description = "Post results to this slack-compatible webhook")]
		public string SlackWebhook { get; set; }
	}
}
