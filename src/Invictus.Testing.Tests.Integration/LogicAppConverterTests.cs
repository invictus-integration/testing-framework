using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Bogus.Extensions;
using Invictus.Testing.LogicApps;
using Invictus.Testing.LogicApps.Model;
using Microsoft.Azure.Management.Logic.Models;
using Newtonsoft.Json;
using Xunit;

namespace Invictus.Testing.Tests.Integration
{
    public class LogicAppConverterTests
    {
        private static readonly Faker BogusGenerator = new Faker();

        [Fact]
        public void ToLogicApp_WithWorkflow_CreatesAlternative()
        {
            // Arrange
            var state = BogusGenerator.PickRandom<LogicAppState>();
            var workflow = new Workflow(
                name: BogusGenerator.Internet.DomainName(),
                createdTime: BogusGenerator.Date.Recent(),
                changedTime: BogusGenerator.Date.Recent(),
                state: state.ToString(),
                version: BogusGenerator.System.Version().ToString(),
                accessEndpoint: BogusGenerator.Internet.IpAddress().ToString().OrNull(BogusGenerator),
                definition: BogusGenerator.Random.String().OrNull(BogusGenerator));

            // Act
            var actual = LogicAppConverter.ToLogicApp(workflow);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(workflow.Name, actual.Name);
            Assert.Equal(workflow.CreatedTime, actual.CreatedTime);
            Assert.Equal(workflow.ChangedTime, actual.ChangedTime);
            Assert.Equal(workflow.State, actual.State.ToString());
            Assert.Equal(workflow.Version, actual.Version);
            Assert.Equal(workflow.AccessEndpoint, actual.AccessEndpoint);
            Assert.Equal(workflow.Definition, actual.Definition);
        }

        [Fact]
        public void ToLogicAppAction_WithInputOutput_CreatesAlternative()
        {
            // Arrange
            var trackedProperties = new Dictionary<string, string>
            {
                [Guid.NewGuid().ToString()] = BogusGenerator.Random.Word()
            };

            string trackedPropertiesJson = JsonConvert.SerializeObject(trackedProperties).OrNull(BogusGenerator);

            var workflowAction = new WorkflowRunAction(
                name: BogusGenerator.Internet.DomainName(),
                startTime: BogusGenerator.Date.Past(),
                endTime: BogusGenerator.Date.Past(),
                status: GenerateStatus(),
                error: BogusGenerator.Random.Bytes(10),
                trackedProperties: trackedPropertiesJson);
            var inputs = BogusGenerator.Random.String();
            var outputs = BogusGenerator.Random.String();

            // Act
            var actual = LogicAppConverter.ToLogicAppAction(workflowAction, inputs, outputs);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(workflowAction.Name, actual.Name);
            Assert.Equal(workflowAction.StartTime, actual.StartTime);
            Assert.Equal(workflowAction.EndTime, actual.EndTime);
            Assert.Equal(workflowAction.Status, actual.Status.ToString());
            Assert.Equal(workflowAction.Error, actual.Error);
            Assert.Equal(inputs, actual.Inputs);
            Assert.Equal(outputs, actual.Outputs);
            Assert.True(trackedPropertiesJson == null || trackedProperties.SequenceEqual(actual.TrackedProperties));
        }

        [Fact]
        public void ToLogicAppRun_WithWorkflowRunAndActions_CreatesCombinedModel()
        {
            // Arrange
            WorkflowRunTrigger trigger = CreateWorkflowRunTrigger();
            WorkflowRun workflowRun = CreateWorkflowRun(trigger);
            IEnumerable<LogicAppAction> actions = CreateLogicAppActions();

            // Act
            var actual = LogicAppConverter.ToLogicAppRun(workflowRun, actions);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(workflowRun.Name, actual.Id);
            Assert.Equal(workflowRun.Status, actual.Status.ToString());
            Assert.Equal(workflowRun.StartTime, actual.StartTime);
            Assert.Equal(workflowRun.EndTime, actual.EndTime);
            Assert.Equal(workflowRun.Error, actual.Error);
            Assert.Equal(workflowRun.Correlation?.ClientTrackingId, actual.CorrelationId);
            Assert.Equal(actions, actual.Actions);

            Assert.Equal(trigger.Name, actual.Trigger.Name);
            Assert.Equal(trigger.Inputs, actual.Trigger.Inputs);
            Assert.Equal(trigger.Outputs, actual.Trigger.Outputs);
            Assert.Equal(trigger.StartTime, actual.Trigger.StartTime);
            Assert.Equal(trigger.EndTime, actual.Trigger.EndTime);
            Assert.Equal(trigger.Status, actual.Trigger.Status.ToString());
            Assert.Equal(trigger.Error, actual.Trigger.Error);

            Assert.All(actions.Where(action => action.TrackedProperties != null), action =>
            {
                Assert.All(action.TrackedProperties, prop => 
                {
                    Assert.Contains(prop, actual.TrackedProperties);
                });
            });
        }

        private static WorkflowRunTrigger CreateWorkflowRunTrigger()
        {
            var trigger = new WorkflowRunTrigger(
                name: BogusGenerator.Internet.DomainName(),
                inputs: BogusGenerator.Random.Word().OrNull(BogusGenerator),
                outputs: BogusGenerator.Random.Word().OrNull(BogusGenerator),
                startTime: BogusGenerator.Date.Past(),
                endTime: BogusGenerator.Date.Recent(),
                status: GenerateStatus(),
                error: BogusGenerator.Random.Bytes(10).OrNull(BogusGenerator));

            return trigger;
        }

        private static WorkflowRun CreateWorkflowRun(WorkflowRunTrigger trigger)
        {
            var correlation = new Correlation(BogusGenerator.Random.String().OrNull(BogusGenerator)).OrNull(BogusGenerator);
            var workflowRun = new WorkflowRun(
                name: BogusGenerator.Internet.DomainWord(),
                startTime: BogusGenerator.Date.Recent(),
                status: GenerateStatus(),
                error: BogusGenerator.Random.Bytes(10).OrNull(BogusGenerator),
                correlation: correlation,
                trigger: trigger);
            return workflowRun;
        }

        private static IEnumerable<LogicAppAction> CreateLogicAppActions()
        {
            int actionCount = BogusGenerator.Random.Int(1, 10);
            int propertyCount = BogusGenerator.Random.Int(1, 10);

            Dictionary<string, string> trackedProperties =
                BogusGenerator.Make(propertyCount, () => new KeyValuePair<string, string>(Guid.NewGuid().ToString(), BogusGenerator.Random.Word()))
                              .ToDictionary(item => item.Key, item => item.Value);

            IList<LogicAppAction> actions = BogusGenerator.Make(actionCount, () =>
            {
                return new LogicAppAction(
                    BogusGenerator.Internet.DomainWord(),
                    BogusGenerator.PickRandom<LogicAppActionStatus>(),
                    BogusGenerator.Random.Words().OrNull(BogusGenerator),
                    BogusGenerator.Random.Words().OrNull(BogusGenerator),
                    BogusGenerator.Random.Byte(10).OrNull(BogusGenerator),
                    trackedProperties,
                    BogusGenerator.Date.Past(),
                    BogusGenerator.Date.Recent());
            });

            return actions;
        }

        private static string GenerateStatus()
        {
            return BogusGenerator.PickRandom<LogicAppActionStatus>().ToString();
        }
    }
}
