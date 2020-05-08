using Newtonsoft.Json;
using System.Collections.Generic;

namespace Invictus.TestLibrary.LogicApps.Model.Definitions
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
        public Dictionary<string, string> TrackedProperties { get; set; }
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
