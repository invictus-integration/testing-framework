using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Invictus.Testing.Model;
using Microsoft.Azure.Management.Logic.Models;
using Newtonsoft.Json.Linq;

namespace Invictus.Testing
{
     public class Converter
    {
        /// <summary>
        /// Convert to LogicAppRun.
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="resourceGroupName"></param>
        /// <param name="logicAppName"></param>
        /// <param name="workFlowRun"></param>
        /// <returns></returns>
        public static LogicAppRun ToLogicAppRun(LogicAppsHelper helper, string resourceGroupName, string logicAppName, WorkflowRun workFlowRun)
        {
            var logicAppRun = (LogicAppRun)workFlowRun;

            logicAppRun.Actions = helper.GetLogicAppRunActionsAsync(resourceGroupName, logicAppName, workFlowRun.Name, false).Result;

            logicAppRun.TrackedProperties = GetAllTrackedProperties(logicAppRun.Actions);

            return logicAppRun;
        }

        /// <summary>
        /// Convert to LogicAppRun.
        /// </summary>
        /// <param name="workFlowRun"></param>
        /// <param name="actions"></param>
        /// <returns></returns>
        public static LogicAppRun ToLogicAppRun(WorkflowRun workFlowRun, List<LogicAppAction> actions)
        {
            var logicAppRun = (LogicAppRun)workFlowRun;

            logicAppRun.Actions = actions;
            logicAppRun.TrackedProperties = GetAllTrackedProperties(logicAppRun.Actions);

            return logicAppRun;
        }

        /// <summary>
        /// Convert to LogicAppAction.
        /// </summary>
        /// <param name="workflowRunAction"></param>
        /// <returns></returns>
        public static async Task<LogicAppAction> ToLogicAppActionAsync(WorkflowRunAction workflowRunAction)
        {
            var logicAppAction = (LogicAppAction)workflowRunAction;

            if (workflowRunAction.InputsLink != null)
            {
                logicAppAction.Inputs = JToken.Parse(await DoHttpRequestAsync(workflowRunAction.InputsLink.Uri));
            }
            if (workflowRunAction.OutputsLink != null)
            {
                logicAppAction.Outputs = JToken.Parse(await DoHttpRequestAsync(workflowRunAction.OutputsLink.Uri));
            }

            return logicAppAction;
        }

        #region Private Methods
        private static async Task<string> DoHttpRequestAsync(string uri)
        {
            string responseString = string.Empty;
            using (var httpClient = new HttpClient())
            {
                responseString = await httpClient.GetStringAsync(uri);
            }

            return responseString;
        }

        private static Dictionary<string, string> GetAllTrackedProperties(List<LogicAppAction> actions)
        {
            return actions
                .Where(x => x.TrackedProperties != null)
                .OrderByDescending(x => x.StartTime)
                .SelectMany(a => a.TrackedProperties)
                .GroupBy(x => x.Key)
                .Select(g => g.First())
                .ToDictionary(x => x.Key, x => x.Value);
        } 
        #endregion
    }
}
