using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Management.Logic.Models;

namespace Invictus.Testing.Model
{
    public class LogicApp
    {
        public string Name { get; set; }
        public DateTimeOffset? CreatedTime { get; set; }
        public DateTimeOffset? ChangedTime { get; set; }
        public string State { get; set; }
        public string Version { get; set; }
        public string AccessEndpoint { get; set; }
        public dynamic Definition { get; set; }

        public static explicit operator LogicApp(Workflow workflow)
        {
            return new LogicApp
            {
                Name = workflow.Name,
                CreatedTime = workflow.CreatedTime,
                ChangedTime = workflow.ChangedTime,
                State = workflow.State,
                Version = workflow.Version,
                AccessEndpoint = workflow.AccessEndpoint,
                Definition = workflow.Definition
            };
        }
    }
}
