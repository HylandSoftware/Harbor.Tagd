using clipr;
using Harbor.Tagd.API;
using Harbor.Tagd.API.Models;
using Harbor.Tagd.Extensions;
using Harbor.Tagd.Notifications;
using Harbor.Tagd.Rules;
using Harbor.Tagd.Util;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Steeltoe.Extensions.Configuration.ConfigServer;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Harbor.Tagd.Tests")]

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
				.Destructure.ByTransforming<Project>(p => new { p.Id, p.Name })
				.Destructure.ByTransforming<Repository>(r => new { r.Id, r.Name, r.TagCount})
				.Destructure.ByTransforming<Tag>(t => new { t.Repository, t.Name, t.CreatedAt })
				.Destructure.ByTransforming<ProcessResult>(r => new { r.RemovedTags, r.IgnoredTags, r.IgnoredRepos, r.IgnoredProjects })
				.WriteTo.Console()
				.CreateLogger();

			try
			{
				var settings = CliParser.Parse<ApplicationSettings>(args);

				logLevel.MinimumLevel = ParseVerbosity(settings.Verbosity);

				var ruleProvider = settings.ConfigFile.IsNullOrEmpty() ?
					new ConfigServerRuleProvider(
						new ConfigServerClientSettings
						{
							Enabled = true,
							FailFast = true,
							Name = APPLICATION_NAME,
							Environment = ENVIRONMENT,
							Uri = settings.ConfigServer,
							Username = settings.ConfigUser,
							Password = settings.ConfigPassword,
							RetryAttempts = 5,
							RetryEnabled = true,
							RetryInitialInterval = 3000,
							RetryMaxInterval = 10000,
							RetryMultiplier = 1.5
						},
						new SerilogLoggerFactory()
					) : (IRuleProvider) new FilesystemRuleProvider(settings.ConfigFile);

				var engine = new TagEngine(
					new HarborClient(NormalizeEndpointUrl(settings.Endpoint)),
					settings,
					Log.ForContext<TagEngine>(),
					ruleProvider,
					settings.SlackWebhook == null ? null : new SlackResultNotifier(settings)
				);

				var sw = new Stopwatch();
				sw.Start();
				await engine.Process();
				Log.Information("Finished in {elapsed}", sw.Elapsed);
			}
			catch(clipr.Core.ParserExit)
			{
				// Don't throw an exception if someone calls help
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
