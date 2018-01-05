using Harbor.Tagd.Extensions;
using Harbor.Tagd.Notifications;
using Harbor.Tagd.Rules;
using Harbormaster;
using Harbormaster.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Harbor.Tagd
{
	public class TagEngine
	{
		private readonly IHarborClient _harbor;
		private readonly HarborSettings _settings;
		private readonly Serilog.ILogger Log;
		private readonly bool _destructive;
		private readonly IRuleProvider _ruleProvider;
		private readonly IResultNotifier _notification;
		
		private RuleSet _ruleSet;

		public TagEngine(IHarborClient harbor, HarborSettings settings, Serilog.ILogger log, IRuleProvider rules, IResultNotifier notifications)
		{
			 _harbor = harbor ?? throw new ArgumentNullException(nameof(harbor));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			Log = log ?? throw new ArgumentNullException(nameof(log));
			_ruleProvider = rules ?? throw new ArgumentNullException(nameof(rules));
			_notification = notifications;

			_destructive = !_settings.ReportOnly;
		}

		public async Task Process()
		{
			LoadRules();
			if(_settings.DumpRules)
			{
				return;
			}

			if (_destructive)
			{
				Log.Warning("Not running in report-only mode. Tags will be deleted");
			}
			else
			{
				Log.Warning("Running in report-only mode. To delete tags, run with --report-only=false");
			}

			try
			{
				Log.Information("Connecting to {endpoint} as {user}", _settings.Endpoint, _settings.Username);
				if(_destructive)
				{
					Log.Warning("Tags will be deleted when matched!");
				}
				else
				{
					Log.Information("Running in report-only mode");
				}
				await _harbor.Login(_settings.Username, _settings.Password);

				var result = new ProcessResult();
				var ignoredProjects = 0;

				foreach(var p in await _harbor.GetAllProjects())
				{
					if(ShouldExcludeProject(p))
					{
						ignoredProjects++;
						Log.Verbose("Skipping project {@project}", p.Name);
						continue;
					}

					var intermediateResult = await ProcessProject(p, _ruleSet.Rules.Where(rule => p.Name.Matches(rule.Project)));
					result = result + intermediateResult;
				}

				result = result + new ProcessResult(ignoredProjects: ignoredProjects);
				Log.Information($"{(_destructive ? "" : "DRY RUN - ")}Tag cleanup complete. Summary: {{@result}}", result);

				await _notification?.Notify(result);
			}
			finally
			{
				if(!string.IsNullOrEmpty(_harbor.SessionToken))
				{
					Log.Information("Logging out");
					await _harbor.Logout();
				}
			}
		}

		private void LoadRules()
		{
			Log.Information("Loading rules using {provider}", _ruleProvider.GetType().FullName);
			_ruleSet = _ruleProvider.Load();

			if ((_ruleSet?.Rules?.Count ?? 0) == 0)
			{
				throw new Exception("The rule provider did not return any tag rules");
			}

			if (_ruleSet.DefaultRule == null)
			{
				throw new Exception("The rule provider did not return a default rule");
			}

			foreach (var rule in _ruleSet.Rules)
			{
				Log.Verbose("Found rule {rule}", rule);
			}
			Log.Verbose("Using default rule {defaultRule}", _ruleSet.DefaultRule);

			Log.Verbose("Ignoring the following projects: {projects}", _ruleSet.IgnoreGlobally?.Projects);
			Log.Verbose("Ignoring the following repos: {repos}", _ruleSet.IgnoreGlobally?.Repos);
			Log.Verbose("Ignoring the following tags: {tags}", _ruleSet.IgnoreGlobally?.Tags);
		}

		private async Task<ProcessResult> ProcessProject(Project p, IEnumerable<Rule> projectRules)
		{
			Log.Information("Processing project {@project}", p);

			var result = new ProcessResult();

			var ignoredRepositories = 0;

			foreach(var r in await _harbor.GetRepositories(p.Id))
			{
				if(ShouldExcludeRepository(r))
				{
					ignoredRepositories++;
					Log.Verbose("Skipping repository {@repository}", r);
					continue;
				}

				var intermediateResult = await ProcessRepository(r, projectRules.Where(rule => r.Name.Matches(rule.Repo)));

				result = result + intermediateResult;
			}

			return result + new ProcessResult(ignoredRepos: ignoredRepositories);
		}

		private async Task<ProcessResult> ProcessRepository(Repository r, IEnumerable<Rule> repoRules)
		{
			Log.Information("Processing repository {@repository}", r);
			var tags = (await _harbor.GetTags(r.Name)).ToList();

			var toIgnore = new HashSet<Tag>();

			var rules = repoRules.ToList();

			foreach (var t in tags)
			{
				if (_ruleSet?.IgnoreGlobally?.Tags.Any(rule => t.Name.Equals(rule, StringComparison.Ordinal)) ?? false)
				{
					Log.Information("Tag {repo}:{name} skipped due to global ignore rules", t.Repository, t.Name);
					toIgnore.Add(t);
					continue;
				}

				if (rules?.SelectMany(rule => rule.Ignore ?? new string[0])?.Any(ignore => t.Name.Equals(ignore, StringComparison.Ordinal)) ?? false)
				{
					Log.Information("Tag {repo}:{name} skipped because it was found in an ignore list that applies to {repo}", t.Repository, t.Name, r.Name);
					toIgnore.Add(t);
					continue;
				}
			}

			var ignoredTags = toIgnore.Count;

			foreach (var t in toIgnore)
			{
				tags.Remove(t);
			}

			var toRemove = new HashSet<Tag>();

			Log.Verbose("{count} rules to process for {repo}", rules.Count, r.Name);
			foreach(var rule in rules)
			{
				Log.Verbose("Processing rule {rule} on repo {repo}", rule, r.Name);
				var matchingTags = tags.Where(t => t.Name.Matches(rule.Tag)).OrderByDescending(t => t.CreatedAt);
				foreach (var tag in matchingTags.Take(rule.Keep))
				{
					Log.Information("Tag {repo}:{name} kept", tag.Repository, tag.Name);
					tags.Remove(tag);
					ignoredTags++;
				}

				foreach (var tag in matchingTags)
				{
					Log.Verbose("Tag {repo}:{name} marked for deletion", tag.Repository, tag.Name);
					tags.Remove(tag);
					toRemove.Add(tag);
				}
			}

			Log.Verbose("Processing default rule on {count} remaining tags for {repo}", tags.Count, r.Name);
			var remainingTags = tags.Where(t => t.Name.Matches(_ruleSet.DefaultRule.Tag)).ToList();
			toRemove.UnionWith(remainingTags);

			foreach (var tag in remainingTags.OrderByDescending(t => t.CreatedAt).Take(_ruleSet.DefaultRule.Keep))
			{
				Log.Information("Tag {repo}:{name} kept by default rule", tag.Repository, tag.Name);
				toRemove.Remove(tag);
				ignoredTags++;
			}

			var removedTags = toRemove.Count;
			foreach(var tag in toRemove)
			{
				Log.Warning($"{(_destructive ? "" : "DRY RUN - ")}Will remove tag {{repo}}:{{name}}", tag.Repository, tag.Name);
			}

			return new ProcessResult(removedTags: removedTags, ignoredTags: ignoredTags);
		}

		private bool ShouldExcludeProject(Project p) =>
			_ruleSet?.IgnoreGlobally?.Projects?.Any(rule => p.Name.Equals(rule, StringComparison.OrdinalIgnoreCase)) ?? false;

		private bool ShouldExcludeRepository(Repository r) =>
			_ruleSet?.IgnoreGlobally?.Repos?.Any(rule => r.Name.Equals(rule, StringComparison.OrdinalIgnoreCase)) ?? false;
	}
}
