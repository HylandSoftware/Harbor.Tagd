﻿using Harbor.Tagd.API;
using Harbor.Tagd.API.Models;
using Harbor.Tagd.Args;
using Harbor.Tagd.Extensions;
using Harbor.Tagd.Notifications;
using Harbor.Tagd.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Harbor.Tagd
{
	public class TagEngine
	{
		private readonly IHarborClient _harbor;
		private readonly ApplicationSettings _settings;
		private readonly Serilog.ILogger Log;
		private readonly bool _destructive;
		private readonly IRuleProvider _ruleProvider;
		private readonly IResultNotifier _notification;

		private readonly TagSet TagSet = new TagSet();
		
		private RuleSet _ruleSet;

		public TagEngine(IHarborClient harbor, ApplicationSettings settings, Serilog.ILogger log, IRuleProvider rules, IResultNotifier notifications)
		{
			 _harbor = harbor ?? throw new ArgumentNullException(nameof(harbor));
			_settings = settings ?? throw new ArgumentNullException(nameof(settings));
			Log = log ?? throw new ArgumentNullException(nameof(log));
			_ruleProvider = rules ?? throw new ArgumentNullException(nameof(rules));
			_notification = notifications;

			_destructive = !_settings.Nondestructive;
		}

		public async Task Process()
		{
			LoadRules();

			if (_destructive)
			{
				Log.Warning("Not running in report-only mode. Tags will be deleted");
			}
			else
			{
				Log.Warning("Running in report-only mode. To delete tags, run with --destructive");
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

				var ignoredProjects = 0;

				var tasks = new List<Task<int>>();
				foreach(var p in await _harbor.GetAllProjects())
				{
					if (ShouldExcludeProject(p))
					{
						Log.Verbose("Skipping project {@project}", p.Name);
						ignoredProjects++;
					}
					else
					{
						tasks.Add(ProcessProject(p, _ruleSet.Rules.Where(rule => p.Name.Matches(rule.Project))));
					}
				};

				var ignoredRepos = (await Task.WhenAll(tasks)).Sum();

				FilterDuplicateTagReferences();

				if(_destructive)
				{
					foreach(var tag in TagSet.ToRemove)
					{
						Log.Warning("Deleting Tag {@tag}", tag);
						await _harbor.DeleteTag(tag.Repository, tag.Name);
					}
				}

				var result = new ProcessResult(TagSet.ToRemove.Count, TagSet.ToKeep.Count, ignoredRepos, ignoredProjects);
				Log.Information($"{(_destructive ? "" : "DRY RUN - ")}Tag cleanup complete. Summary: {{@result}}", result);

				await (_notification?.Notify(result) ?? Task.CompletedTask);
			}
			catch(Exception ex)
			{
				await (_notification?.NotifyUnhandledException(ex) ?? Task.CompletedTask);

				throw;
			}
			finally
			{
				Log.Information("Logging out");
				await _harbor.Logout();
			}
		}

		private void LoadRules()
		{
			Log.Information("Loading rules using {provider}", _ruleProvider.GetType().FullName);
			_ruleSet = _ruleProvider.Load()?.Check() ?? throw new ArgumentException("The rule provider did not provide any rules");
		}

		private async Task<int> ProcessProject(Project p, IEnumerable<Rule> projectRules)
		{
			Log.Information("Processing project {@project}", p);

			var ignoredRepos = 0;

			var tasks = new List<Task>();
			foreach(var r in await _harbor.GetRepositories(p.Id))
			{
				if (ShouldExcludeRepository(r))
				{
					Log.Verbose("Skipping repository {@repository}", r);
					ignoredRepos++;
				}
				else
				{
					tasks.Add(ProcessRepository(r, projectRules.Where(rule => r.Name.Matches(rule.Repo))));
				}
			};

			await Task.WhenAll(tasks);

			return ignoredRepos;
		}

		private async Task ProcessRepository(Repository r, IEnumerable<Rule> repoRules)
		{
			Log.Information("Processing repository {@repository}", r);

			var rules = repoRules.ToList();
			foreach (var t in await _harbor.GetTags(r.Name))
			{
				if (string.IsNullOrEmpty(t.Digest))
				{
					Log.Warning("Tag {repo}:{name} does not have a digest and was likely corrupted during a delete operation. This tag will be skipped. See https://github.com/vmware/harbor/issues/4214 for details", t.Repository, t.Name);
				}
				else if (_ruleSet?.IgnoreGlobally?.Tags.Any(rule => t.Name.Equals(rule, StringComparison.Ordinal)) ?? false)
				{
					Log.Information("Tag {repo}:{name} skipped due to global ignore rules", t.Repository, t.Name);
					TagSet.ToKeep.Add(t);
				}
				else if ((_ruleSet.DefaultRule?.Ignore ?? new string[0]).Union(rules?.SelectMany(rule => rule.Ignore ?? new string[0]))?.Any(ignore => t.Name.Equals(ignore, StringComparison.Ordinal)) ?? false)
				{
					Log.Information("Tag {repo}:{name} skipped because it was found in an ignore list that applies to {repo}", t.Repository, t.Name, r.Name);
					TagSet.ToKeep.Add(t);
				}
				else
				{
					TagSet.Tags.Add(t);
				}
			}

			Log.Verbose("{count} rules to process for {repo}", rules.Count, r.Name);
			foreach(var rule in rules)
			{
				Log.Verbose("Processing rule {rule} on repo {repo}", rule, r.Name);
				var matchingTags = TagSet.Tags.Where(t => t.Name.Matches(rule.Tag)).OrderByDescending(t => t.CreatedAt);
				foreach (var tag in matchingTags.Take(rule.Keep))
				{
					Log.Information("Tag {repo}:{name} kept", tag.Repository, tag.Name);
					TagSet.Tags.Remove(tag);
					TagSet.ToKeep.Add(tag);
				}

				foreach (var tag in matchingTags)
				{
					Log.Verbose("Tag {repo}:{name} marked for deletion", tag.Repository, tag.Name);
					TagSet.Tags.Remove(tag);
					TagSet.ToRemove.Add(tag);
				}
			}

			Log.Verbose("Processing default rule on {count} remaining tags for {repo}", TagSet.Tags.Count, r.Name);
			var remainingTags = TagSet.Tags.Where(t => t.Name.Matches(_ruleSet.DefaultRule.Tag)).ToList();
			foreach(var t in remainingTags)
			{
				TagSet.Tags.Remove(t);
				TagSet.ToRemove.Add(t);
			}

			foreach (var tag in remainingTags.OrderByDescending(t => t.CreatedAt).Take(_ruleSet.DefaultRule.Keep))
			{
				Log.Information("Tag {repo}:{name} kept by default rule", tag.Repository, tag.Name);
				TagSet.ToRemove.Remove(tag);
				TagSet.ToKeep.Add(tag);
			}

			if(TagSet.Tags.Count > 0)
			{
				Log.Warning("The default rule did not match all remaining tags for {@repo}. {count} remaining tags will be kept", r, TagSet.Tags.Count);
				TagSet.ToKeep.UnionWith(TagSet.Tags);
				TagSet.Tags.Clear();
			}
		}

		private void FilterDuplicateTagReferences()
		{
			Log.Information("Keeping tags that refer to the same image as an explicitly kept or ignored tag");
			foreach(var t in TagSet.ToKeep)
			{
				foreach(var duplicate in TagSet.ToRemove.Where(tr => tr.Digest.Equals(t.Digest, StringComparison.OrdinalIgnoreCase)).ToList())
				{
					Log.Verbose("Will not delete {@duplicate} because it has the same digest as kept tag {@t}", duplicate, t);
					TagSet.ToRemove.Remove(duplicate);
				}
			}
		}

		private bool ShouldExcludeProject(Project p) =>
			_ruleSet?.IgnoreGlobally?.Projects?.Any(rule => p.Name.Equals(rule, StringComparison.OrdinalIgnoreCase)) ?? false;

		private bool ShouldExcludeRepository(Repository r) =>
			_ruleSet?.IgnoreGlobally?.Repos?.Any(rule => r.Name.Equals(rule, StringComparison.OrdinalIgnoreCase)) ?? false;
	}
}
