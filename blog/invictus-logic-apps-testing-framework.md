Anouncing the Invictus Test Framework for Azure Logic Apps
--
>While writing unit or integration tests should be part of every project, this proofs to be quite difficult whenever creating integration scenarios using Azure Logic Apps.
>Thanks to the creation of the Invictus Test Framework for Azure Logic Apps, there is no longer any excuse not to write any test-cases.

Up untill now, when building interfaces on top of Azure Logic Apps, you were forced to manually (re)test every implemented feature because there simply wasn't anything available to help you on your way, which ends up costing both time and money you'd like to spend elsewhere.  

Since developing the Invictus Methodology - which includes patterns, best practices and several templates - Codit has been investing in building a solid foundation enabling us to quickly kick off new projects, while ensuring a high level of quality. The biggest thing which, untill now, was still lacking, was the ability to include test-cases into those projects.  
Because of this, we started working on a set of classes allowing us to easily enable/disable, trigger and even modify existing Azure Logic Apps. But, since the aforementioned operations are not enough to write full-blown test-scenarios, we extended this list to ensure you would also be able to actually monitor the executed Logic App-runs.

All of these operations have now been forged into a test framework for Azure Logic Apps, which has been made available on NuGet and can be included in your project using the following command:

```shell
> Install-Package Invictus.Testing.LogicApps
```

### Controlling an Azure Logic App

One of the first and most important aspects of this framework is the ability to have control over every single Logic App that is part of your interface, only by specifying the name of the resource group and the targetted Logic App. Of course you also need to authenticate, for which you will need to provide a Client-ID and Client-Secret along with the details of Azure Subscription.  

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
    await using (await logicApp.TemporaryEnableAsync())
    {
        // Perform other actions related to your test-case.
    }
}
```

### Monitoring an Azure Logic App

After preparing the interface for a specific scenario and actually triggering the test, it is of course as important to be able to retrieve enough information to define the outcome of the test.  
Which is why, in addition to the operations listed above you are also able to:  
- Get a Logic App run by 'correlation ID' a.k.a. the 'client tracking ID'
- Get a Logic App run by tracked properties (1 or many)
- Get a Logic App run within a specific time-frame
- Get a given number of Logic App runs once completed (polling).   
  *In case you have the same Logic App running multiple times in parrallel.* 

When we talk about getting a Logic App run, as mentioned above, this means you will be able to access all metadata of this run, including every possible piece of information, such as the status of the executed actions along with the tracked properties per action as well as the global overview of these properties.

To give you an idea on how this works, a small code-sample has been included to show how you can poll for a Logic App run:
```csharp
LogicAppRun logicAppRun =
    await LogicAppsProvider.LocatedAt(resourceGroup, logicAppName, authentication)
                           .WithTimeout(TimeSpan.FromSeconds(15))
                           .WithTrackedProperty("OrderNumber", "123456")
                           .PollForSingleLogicAppRunAsync();
```


Hoping that this will enable and simplify the process of including test-cases within every single integration project on Azure Logic Apps, we are looking forward to receive feedback allowing us to keep improving and extending our Invictus Test Framework for Azure Logic Apps.

*Note: all documentation can be found on [this page](https://invictus-integration.github.io/testing-framework/#/).*