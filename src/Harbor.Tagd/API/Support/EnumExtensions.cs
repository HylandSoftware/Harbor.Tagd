using System;
using System.Collections.Generic;

namespace Harbor.Tagd.API.Support
{
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Modify<T>(this IEnumerable<T> src, Action<T> modifier)
		{
			foreach (var t in src)
			{
				modifier(t);
				yield return t;
			}
		}
	}
}
