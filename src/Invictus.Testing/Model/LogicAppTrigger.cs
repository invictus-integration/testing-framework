using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Management.Logic.Models;

namespace Invictus.Testing.Model
{
    public class LogicAppTrigger
    {
        public string Name { get; set; }
        public object Inputs { get; set; }
        public object Outputs { get; set; }
        public DateTimeOffset? StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public string Status { get; set; }
        public object Error { get; set; }

        public static explicit operator LogicAppTrigger(WorkflowRunTrigger workflowRunTrigger)
        {
            return new LogicAppTrigger
            {
                Name = workflowRunTrigger.Name,
                Inputs = workflowRunTrigger.Inputs,
                Outputs = workflowRunTrigger.Outputs,
                StartTime = workflowRunTrigger.StartTime,
                EndTime = workflowRunTrigger.EndTime,
                Status = workflowRunTrigger.Status,
                Error = workflowRunTrigger.Error,
            };
        }
    }
}
