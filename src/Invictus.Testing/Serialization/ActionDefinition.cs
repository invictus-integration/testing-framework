using System.Collections.Generic;
using Newtonsoft.Json;

namespace Invictus.Testing.Serialization
{
    public class ActionDefinition
    {
        [JsonProperty("runAfter")]
        public Dictionary<string, dynamic> RunAfter { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("inputs")]
        public Dictionary<string, dynamic> Inputs { get; set; }

        [JsonProperty("runtimeConfiguration")]
        public RuntimeConfiguration RuntimeConfiguration { get; set; }

        [JsonProperty("trackedProperties")]
        public dynamic TrackedProperties { get; set; }

        [JsonProperty("actions")]
        public Dictionary<string, ActionDefinition> Actions { get; set; }

        [JsonProperty("expression")]
        public Dictionary<string, dynamic> Expressions { get; set; }
    }

    public class RuntimeConfiguration
    {
        [JsonProperty("staticResult")]
        public StaticResult StaticResult { get; set; }
    }

    public class StaticResult
    {
        [JsonProperty("staticResultOptions")]
        public string StaticResultOptions { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}
