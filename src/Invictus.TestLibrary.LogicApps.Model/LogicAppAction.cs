using Microsoft.Azure.Management.Logic.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Invictus.TestLibrary.LogicApps.Model
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

        public static explicit operator LogicAppAction(WorkflowRunAction workflowRunAction)
        {
            return new LogicAppAction
            {
                Name = workflowRunAction.Name,
                StartTime = workflowRunAction.StartTime,
                EndTime = workflowRunAction.EndTime,
                Status = workflowRunAction.Status,
                Error = workflowRunAction.Error,
                TrackedProperties = workflowRunAction.TrackedProperties != null
                                ? JsonConvert.DeserializeObject<Dictionary<string, string>>(workflowRunAction.TrackedProperties.ToString())
                                : null
            };
        }
    }
}
