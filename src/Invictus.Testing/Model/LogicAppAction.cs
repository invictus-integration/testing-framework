using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Management.Logic.Models;
using Newtonsoft.Json;

namespace Invictus.Testing.Model
{
    public class LogicAppAction
    {
        public string Name { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public object Error { get; set; }
        public dynamic Inputs { get; set; }
        public dynamic Outputs { get; set; }
        public Dictionary<string, string> TrackedProperties { get; set; }
    }
}
