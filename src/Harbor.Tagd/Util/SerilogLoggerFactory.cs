using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
using System;

namespace Harbor.Tagd.Util
{
	internal class SerilogLoggerFactory : ILoggerFactory
	{
		private readonly ILoggerProvider _provider = new SerilogLoggerProvider(Serilog.Log.Logger);

		public void AddProvider(ILoggerProvider provider)
		{
		}

		public ILogger CreateLogger(string categoryName) => _provider.CreateLogger(categoryName);

		public void Dispose()
		{
			throw new NotImplementedException();
		}
	}
}
