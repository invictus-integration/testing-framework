Announcing the Invictus Test Framework for Azure Logic Apps
--
>While writing unit or integration tests should be part of every project, this proofs to be quite difficult whenever creating integration scenarios using Azure Logic Apps.
>Thanks to the creation of the Invictus Test Framework for Azure Logic Apps, there is no longer any excuse not to write any test-cases.

Up until now, when building interfaces on top of Azure Logic Apps, you were forced to manually (re)test every implemented feature because there simply wasn't anything available to help you on your way, which ends up costing both time and money you'd like to spend elsewhere.  

Since developing the Invictus Methodology - which includes patterns, best practices and several templates - Codit has been investing in building a solid foundation enabling us to quickly kick off new projects, while ensuring a high level of quality. The biggest thing which, until now, was still lacking, was the ability to include test-cases into those projects.  
Because of this, we started working on a set of classes allowing us to easily enable/disable, trigger and even modify existing Azure Logic Apps. But, since the operations are not enough to write full-blown test-scenarios, we extended this list to ensure you would also be able to monitor the executed Logic App-runs.

All these operations have now been forged into a test framework for Azure Logic Apps, which has been made available on NuGet and can be included in your project using the following command:

```shell
> Install-Package Invictus.Testing.LogicApps
```

### Authentication
When you want to use this package to track of control your Logic Apps, you will have to authenticate against the subscription containing said Logic Apps of course.  
This can be done by re-using the existing service principal, which has been created to connect your Azure DevOps project to the Azure subscription. However, seeing as this service principal typically has contributor-access to the entire subscription, which is quite an overkill for testing-purposes, it would be advised to create a new service principal, entirely dedicated to the testing scenarios.  
This test-service principal will need to get the "*Logic App Contributor*"-role assigned, as any other role would be insufficient to update your Logic Apps (to enable static results, for instance).  

But what exactly do you need to specify in order to really authenticate?  
Well, below is an overview of the required information:

```csharp
// The Tenant ID as found in the AAD properties
string tenantId = "my-tenant-id";
// The ID of the subscription containing your Logic Apps.
string subscriptionId = "my-subscription-id";
// The Client ID of the service principal to be used to authenticate.
string clientId = "my-client-id";
// The Client Secret created along with the service principal used for authentication.
// You might want to look at Arcus.Security.Provides.AzureKeyVault to ensure this value can be stored in Azure Key Vault instead
string clientSecret = "my-client-secret";
// The name of the resource group containing the Logic App you want to control
string resourceGroup = "my-resource-group";
// The name of the Logic App you want to control.
string logicAppName = "my-logic-app-name";

// Use the LogicAppAuthentication-class to retrieve the required access token.
var authentication = LogicAppAuthentication.UsingServicePrincipal(tenantId, subscriptionId, clientId, clientSecret);
// Once authenticated, use this token to get access to your Logic Apps.
using (var logicApp = await LogicAppClient.CreateAsync(resourceGroup, logicAppName, authentication))
{
}
```

*Want to read more about how to authenticate and gain access to your Logic Apps?*  
*Have a look on [this page](https://invictus-integration.github.io/testing-framework/#/logic-apps/authentication).*  

### Controlling an Azure Logic App

One of the first and most important aspects of this framework is the ability to have control over every single Logic App that is part of your interface, only by specifying the name of the resource group and the targeted Logic App. Of course, you also need to authenticate, for which you will need to provide the authentication-token which has been retrieved by the previously mentioned operation.  

But, once the **LogicAppClient**-object has been created, you can perform any of these operations:  
- Create a Logic App
- Get the Logic App metadata  
- Get the Logic App trigger-URL  
- (Temporarily) enable the Logic App  
- (Temporarily) update the Logic App definition  
- (Temporarily) enable static results on specific actions within the Logic App  
- Trigger a Logic App run  
- Delete a Logic App  

All of the above operations should provide sufficient possibilities for you to adjust and trigger your Logic Apps to allow for a specific scenario to be tested.

Below is a small example of how you can enable a Logic App during the execution of your test:
```csharp
using (var logicApp = await LogicAppClient.CreateAsync(resourceGroup, logicAppName, authentication))
{
    // Any action to be performed before enabling the Logic App.
    await using (await logicApp.TemporaryEnableAsync())
    {
        // Perform actions related to your test-case, while the Logic App is enabled.
    }
    // Any action to be performed after disabling the Logic App.
}
```

*Want to read more about how to control a Logic App?*  
*Have a look on [this page](https://invictus-integration.github.io/testing-framework/#/logic-apps/control-single-logicapp).*  

### Monitoring an Azure Logic App

After preparing the interface for a specific scenario and actually triggering the test, it is of course as important to be able to retrieve enough information to define the outcome of the test.  
Which is why, in addition to the operations listed above you are also able to:  
- Get a Logic App run by 'correlation ID' a.k.a. the 'client tracking ID'
- Get a Logic App run by tracked properties (1 or many)
- Get a Logic App run within a specific timeframe
- Get a given number of Logic App runs once completed (polling).   
  *In case you have the same Logic App running multiple times in parallel.* 

When we talk about getting a Logic App run, as mentioned above, this means you will be able to access all metadata of this run, including every possible piece of information, such as the status of the executed actions along with the tracked properties per action as well as the global overview of these properties.

To give you an idea on how you can use this framework to monitor Logic App runs, some small code-samples have been included.  
In this first example we will poll for 15 seconds and look for a single run where the _Correlation Id_ matches a specific value:
```csharp
LogicAppRun logicAppRun =
    await LogicAppsProvider.LocatedAt(resourceGroup, logicAppName, authentication)
                           .WithTimeout(TimeSpan.FromSeconds(15))
                           .WithCorrelationId("08586073923413753771945113291CU110")
                           .PollForSingleLogicAppRunAsync();
```

Next, let's try and retrieve all Logic App runs - we're expecting to see 3 of them - within a timeframe of 60 seconds, all of whom should be containing the tracked property _OrderNumber_ matching the value _123456_:
```csharp
IEnumerable<LogicAppRun> logicAppRuns =
    await LogicAppsProvider.LocatedAt(resourceGroup, logicAppName, authentication)
                           .WithTimeout(TimeSpan.FromSeconds(60))
                           .WithTrackedProperty("OrderNumber", "123456")
                           .PollForLogicAppRunsAsync(minimumNumberOfItems: 3);
```

*Want to read more about how to monitor a Logic App?*  
*Have a look on [this page](https://invictus-integration.github.io/testing-framework/#/logic-apps/polling-logicapp-runs).*  


# Conclusion
Hoping that this will enable and simplify the process of including test-cases within every single integration project on Azure Logic Apps, we are looking forward to receiving feedback allowing us to keep improving and extending our Invictus Test Framework for Azure Logic Apps.