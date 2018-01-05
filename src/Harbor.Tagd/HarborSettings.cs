namespace Harbor.Tagd
{
	public class HarborSettings
	{
		public string Endpoint { get; set; }
		public string Username { get; set; }
		public string Password { get; set; }
		public string Verbosity { get; set; } = "info";
		public string ChatHook { get; set; }

		public string ConfigServer { get; set; } = "http://localhost:8888";
		public string ConfigUser { get; set; }
		public string ConfigPassword { get; set; }

		public bool ReportOnly { get; set; } = true;
		public bool DumpRules { get; set; } = false;

		public string SlackWebhook { get; set; }
	}
}
