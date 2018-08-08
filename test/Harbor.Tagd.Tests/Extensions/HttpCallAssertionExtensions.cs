using Flurl.Http.Testing;
using Harbor.Tagd.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Text.RegularExpressions;

namespace Harbor.Tagd.Tests.Extensions
{
	internal static class HttpCallAssertionExtensions
	{
		public static HttpCallAssertion WithJsonProperty<TProperty>(this HttpCallAssertion a, Func<dynamic, TProperty> propertyPicker, TProperty expected)
			where TProperty : IEquatable<TProperty> => a.With(r => ((TProperty)propertyPicker.Invoke(JObject.Parse(r.RequestBody))).Equals(expected));

		public static HttpCallAssertion WithJsonPropertyMatching(this HttpCallAssertion a, Func<dynamic, string> propertyPicker, string regex) =>
			a.With(r => propertyPicker.Invoke(JObject.Parse(r.RequestBody)).Matches(new Regex(regex)));
	}
}
