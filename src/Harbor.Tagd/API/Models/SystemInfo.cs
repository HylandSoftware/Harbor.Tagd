using Newtonsoft.Json;

namespace Harbor.Tagd.API.Models
{
    public class SystemInfo
    {
        [JsonProperty("harbor_version")]
        public string Version { get; set; }
    }
}