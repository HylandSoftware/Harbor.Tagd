using clipr;

namespace Harbor.Tagd.Args
{
	[ApplicationInfo(Name = "tagd check", Description = "Load and validate rules")]
	public class CommonArgs
	{
		private const string CONFIG_METHOD = "config-method";

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

		[NamedArgument("insecure-disable-certificate-validation", Description = "Don't validate server certificates for Harbor", Action = ParseAction.StoreTrue)]
		public bool DisableCertificateValidation { get; set; }
	}
}
