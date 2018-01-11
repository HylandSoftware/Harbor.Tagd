using System;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Harbor.Tagd.Rules
{
	internal class FilesystemRuleProvider : IRuleProvider
	{
		internal class RegexTypeConverter : IYamlTypeConverter
		{
			public bool Accepts(Type type) => type == typeof(Regex);

			public object ReadYaml(IParser parser, Type type)
			{
				var result = new Regex(((Scalar)parser.Current).Value, RegexOptions.Compiled);
				parser.MoveNext();

				return result;
			}

			public void WriteYaml(IEmitter emitter, object value, Type type) =>
				emitter.Emit(new Scalar(((Regex)value).ToString()));
		}

		private readonly string _path;

		public FilesystemRuleProvider(string path) => _path = path ?? throw new ArgumentNullException(nameof(path));

		public RuleSet Load() => new DeserializerBuilder()
			.WithNamingConvention(new CamelCaseNamingConvention())
			.WithTypeConverter(new RegexTypeConverter())
			.Build()
			.Deserialize<RuleSet>(File.ReadAllText(_path))
			.EnsureDefaults();
	}
}
