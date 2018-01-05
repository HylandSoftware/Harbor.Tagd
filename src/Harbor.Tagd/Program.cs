using Harbor.Tagd.Notifications;
using Harbor.Tagd.Rules;
using Harbor.Tagd.Util;
using Harbormaster;
using Harbormaster.Models;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Harbor.Tagd
{
	public class Program
	{
#if DEBUG
		private const string ENVIRONMENT = "Development";
#else
		private const string ENVIRONMENT = "Production";
#endif
		private const string APPLICATION_NAME = "tagd";

		public static async Task Main(string[] args)
		{
			var logLevel = new LoggingLevelSwitch(LogEventLevel.Information);

			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.ControlledBy(logLevel)
				.Enrich.FromLogContext()
				.Destructure.ByTransforming<Project>(p => new { p.Id, p.Name, p.OwnerId })
				.Destructure.ByTransforming<Repository>(r => new { r.Id, r.Name, r.TagCount})
				.Destructure.ByTransforming<Tag>(t => new { t.Repository, t.Name, t.CreatedAt })
				.Destructure.ByTransforming<ProcessResult>(r => new { r.RemovedTags, r.IgnoredTags, r.IgnoredRepos, r.IgnoredProjects })
				.WriteTo.Console()
				.CreateLogger();

			try
			{
				var config = new ConfigurationBuilder()
					.AddCommandLine(args, new Dictionary<string, string>
					{
						{ "--config-server", "ConfigServer" },
						{ "--config-user", "ConfigUser" },
						{ "--config-password", "ConfigPassword" },
						{ "--report-only", "ReportOnly" },
						{ "--dump-rules", "DumpRules" },
						{ "--notify-slack", "SlackWebhook" }
					})
					.Build();

				var settings = new HarborSettings();
				config.Bind(settings);

				logLevel.MinimumLevel = ParseVerbosity(settings.Verbosity);

				var engine = new TagEngine(
					new HarborClient(NormalizeEndpointUrl(settings.Endpoint)),
					settings,
					Log.ForContext<TagEngine>(),
					new ConfigServerRuleProvider(
						new ConfigServerClientSettings
						{
							Enabled = true,
							FailFast = true,
							Name = APPLICATION_NAME,
							Environment = ENVIRONMENT,
							Uri = settings.ConfigServer,
							Username = settings.ConfigUser,
							Password = settings.ConfigPassword
						},
						new SerilogLoggerFactory()
					),
					settings.SlackWebhook == null ? null : new SlackResultNotifier(settings)
				);

				var sw = new Stopwatch();
				sw.Start();
				await engine.Process();
				Log.Information("Finished in {elapsed}", sw.Elapsed);
			}
			catch(Exception ex)
			{
				Log.Fatal(ex, "An Error was encountered while processing tags");
			}
			finally
			{
				Log.CloseAndFlush();
			}
		}

		private static string NormalizeEndpointUrl(string endpoint) =>
			(endpoint.ToLower().StartsWith("http") ? endpoint : $"https://{endpoint}").TrimEnd('/');

		private static LogEventLevel ParseVerbosity(string verbosity)
		{
			switch(verbosity?.ToLower())
			{
				case "v":
				case "verbose": return LogEventLevel.Verbose;

				case "d":
				case "debug": return LogEventLevel.Debug;

				case "i":
				case "info":
				case "information": return LogEventLevel.Information;

				case "w":
				case "warn":
				case "warning": return LogEventLevel.Warning;

				case "e":
				case "err":
				case "error": return LogEventLevel.Error;

				case "f":
				case "fatal": return LogEventLevel.Fatal;
				
				default:  throw new ArgumentException($"Unknown log level {verbosity}", nameof(verbosity));
			}
		}
	}
}
