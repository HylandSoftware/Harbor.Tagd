using System;

namespace Harbor.Tagd.Extensions
{
	public static class IntExtensions
	{
		public static int EnsurePositive(this int input) => input >= 0 ? input : throw new ArgumentException("Input must be positive", nameof(input));
	}
}
