using Newtonsoft.Json;
using System.Collections.Generic;

namespace Invictus.TestLibrary.LogicApps.Model.Definitions
{
    public class LogicAppDefinition
    {
        [JsonProperty("$schema")]
        public string Schema { get; set; }

        [JsonProperty("triggers")]
        public Dictionary<string, dynamic> Triggers { get; set; }

        [JsonProperty("actions")]
        public Dictionary<string, ActionDefinition> Actions { get; set; }

        [JsonProperty("contentVersion")]
        public string ContentVersion { get; set; }

        [JsonProperty("outputs")]
        public Dictionary<string, dynamic> Outputs { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, dynamic> Parameters { get; set; }

        [JsonProperty("staticResults")]
        public Dictionary<string, StaticResultDefinition> StaticResults { get; set; }
    }
}
