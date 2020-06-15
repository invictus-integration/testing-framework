using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Management.Logic.Models;

namespace Invictus.Testing.Model
{
    public class LogicAppRun
    {
        public string Id { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public string Status { get; set; }
        public object Error { get; set; }
        public string CorrelationId { get; set; }
        public LogicAppTrigger Trigger { get; set; }
        public IEnumerable<LogicAppAction> Actions { get; set; }
        public IReadOnlyDictionary<string, string> TrackedProperties { get; set; }
    }
}
