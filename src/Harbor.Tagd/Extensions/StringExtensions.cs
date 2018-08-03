using System.Text.RegularExpressions;

namespace Harbor.Tagd.Extensions
{
	public static class StringExtensions
	{
		public static Regex ToCompiledRegex(this string input, RegexOptions options = RegexOptions.None) =>
			string.IsNullOrEmpty(input) ? null : new Regex(input, options | RegexOptions.Compiled);

		public static bool Matches(this string input, Regex regex) => regex.IsMatch(input);
	}
}
