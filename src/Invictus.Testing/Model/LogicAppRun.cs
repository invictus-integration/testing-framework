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
        public List<LogicAppAction> Actions { get; set; }
        public Dictionary<string, string> TrackedProperties { get; set; }

        public static explicit operator LogicAppRun(WorkflowRun workFlowRun)
        {
            return new LogicAppRun
            {
                Id = workFlowRun.Name,
                StartTime = workFlowRun.StartTime,
                EndTime = workFlowRun.EndTime,
                Status = workFlowRun.Status,
                Error = workFlowRun.Error,
                CorrelationId = workFlowRun.Correlation?.ClientTrackingId,
                Trigger = (LogicAppTrigger)workFlowRun.Trigger
            };
        }
    }
}
