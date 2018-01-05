using System.Text.RegularExpressions;

namespace Harbor.Tagd.Extensions
{
	public static class StringExtensions
	{
		public static bool IsNullOrEmpty(this string input) => string.IsNullOrEmpty(input);

		public static bool IsNullOrWhiteSpace(this string input) => string.IsNullOrWhiteSpace(input);

		public static Regex ToCompiledRegex(this string input, RegexOptions options = RegexOptions.None) =>
			new Regex(input, options | RegexOptions.Compiled);

		public static bool Matches(this string input, string regex, RegexOptions options = RegexOptions.None) =>
			input.Matches(new Regex(regex, options));

		public static bool Matches(this string input, Regex regex) => regex.IsMatch(input);
	}
}
