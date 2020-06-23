using System.Collections.Generic;
using Newtonsoft.Json;

namespace Invictus.Testing.LogicApps.Serialization
{
    public class StaticResultDefinition
    {
        [JsonProperty("outputs")]
        public Outputs Outputs { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }

    public partial class Outputs
    {
        [JsonProperty("headers")]
        public Dictionary<string, string> Headers { get; set; }

        [JsonProperty("statusCode")]
        public string StatusCode { get; set; }

        [JsonProperty("body")]
        public dynamic Body { get; set; }
    }
}
