using System.Text.RegularExpressions;

namespace Harbor.Tagd.Rules
{
	public class Rule
	{
		public Regex Project { get; set; }
		public Regex Repo { get; set; }
		public Regex Tag { get; set; }
		public string[] Ignore { get; set; }
		public int Keep { get; set; }

		public override string ToString() =>
			$"{{'Project': '{Project}', 'Repo': '{Repo}', 'Tag': '{Tag}', 'Keep': {Keep}, 'Ignore': [{string.Join(',', Ignore ?? new string[0])}]}}";
	}
}
