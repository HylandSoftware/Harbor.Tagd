using System.Net.Http;
using Flurl.Http.Configuration;
using Serilog;

namespace Harbor.Tagd.Util
{
	internal class InsecureHttpClientFactory : DefaultHttpClientFactory
	{
		public override HttpMessageHandler CreateMessageHandler()
		{
			var result = base.CreateMessageHandler();

			if (result is HttpClientHandler http)
			{
				http.ServerCertificateCustomValidationCallback = (_, __, ___, ____) => true;
			}
			else
			{
				Log.Error("Unknown Message Handler Type {type}", result.GetType().FullName);
			}

			return result;
		}
	}
}
