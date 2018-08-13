using AutoFixture.Xunit2;
using Serilog.Events;
using System;
using Xunit;

namespace Harbor.Tagd.Tests
{
	public class ProgramTests
	{
		[Theory]
		[InlineData("v", LogEventLevel.Verbose)]
		[InlineData("verbose", LogEventLevel.Verbose)]
		[InlineData("d", LogEventLevel.Debug)]
		[InlineData("debug", LogEventLevel.Debug)]
		[InlineData("i", LogEventLevel.Information)]
		[InlineData("info", LogEventLevel.Information)]
		[InlineData("information", LogEventLevel.Information)]
		[InlineData("w", LogEventLevel.Warning)]
		[InlineData("warn", LogEventLevel.Warning)]
		[InlineData("warning", LogEventLevel.Warning)]
		[InlineData("e", LogEventLevel.Error)]
		[InlineData("err", LogEventLevel.Error)]
		[InlineData("error", LogEventLevel.Error)]
		[InlineData("f", LogEventLevel.Fatal)]
		[InlineData("fatal", LogEventLevel.Fatal)]
		public void CanParseVerbosity(string verbosity, LogEventLevel expected) =>
			Assert.Equal(expected, Program.ParseVerbosity(verbosity));

		[Theory, AutoData]
		public void ArgumentExceptionForUnknownVerbosity(string verbosity) =>
			Assert.Throws<ArgumentException>(() => Program.ParseVerbosity(verbosity));

		[Theory]
		[InlineData("http://foo/bar", "http://foo/bar")]
		[InlineData("https://foo/bar", "https://foo/bar")]
		[InlineData("foo/bar", "https://foo/bar")]
		public void NormalizesEndpoint(string endpoint, string expected) =>
			Assert.Equal(expected, Program.NormalizeEndpointUrl(endpoint));
	}
}
