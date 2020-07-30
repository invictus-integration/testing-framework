Announcing the Invictus Test Framework for Azure Logic Apps
--
>While writing unit or integration tests should be part of every project, this proofs to be quite difficult whenever creating integration scenarios using Azure Logic Apps.
>Thanks to the creation of the Invictus Test Framework for Azure Logic Apps, there is no longer any excuse not to write any test-cases.

Up until now, when building interfaces on top of Azure Logic Apps, you were forced to manually (re)test every implemented feature because there simply wasn't anything available to help you on your way.  

While this approach works fine, we've found that it has a few flaws:  
- New people on your team don't have an easy way to understand how flows should work  
- It's cumbersome since you cannot easily automate it to guarantee quality  
- It takes time & effort which ends up in a higher engineering & testing cost, money you'd like to spend elsewhere  

Since developing the Invictus Methodology, which includes patterns, best practices and several templates, Codit has been investing in building a solid foundation enabling us to quickly kick-off new projects, while ensuring a high level of quality. The biggest thing which, until now, was still lacking, was the ability to include test-cases into those projects.  
Because of this, we started working on a framework that allowed us to easily enable/disable, trigger and even modify existing Azure Logic Apps. But, since the operations are not enough to write full-blown test-scenarios, we extended this list to ensure you would also be able to monitor the executed Azure Logic App runs.  

All these operations have now been forged into the Invictus Test Framework for Azure Logic Apps which we used internally for our customers and have proven to be effective.  
Today, **we are happy to announce that we are open-sourcing Invictus Test Framework for Azure Logic Apps on GitHub** and it is now available on NuGet for you to use on your projects!  

Get started very easily :  
```shell
> Install-Package Invictus.Testing.LogicApps
```

### Authentication
When you want to use this package to track of control your Logic Apps, you will have to authenticate against your Azure subscription containing said Logic Apps of course.  
As of today, we provide support for using service principal authentication! Our framework requires to have at least [*Logic App Contributor*](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#logic-app-contributor) which gives enough permissions to do our magic.  

Of course, you can re-use existing service principals that you already have in Azure DevOps for example. However, seeing as this service principal typically has contributor-access to the entire subscription, which is quite an overkill for testing-purposes, it would be advised to create a new service principal, entirely dedicated to the testing scenarios.  

But what exactly do you need to specify in order to really authenticate?  
Well, below is an overview of the required information:

```csharp
// The Tenant ID of the Azure AD
string tenantId = "my-tenant-id";
// The ID of the subscription containing your Azure Logic App.
string subscriptionId = "my-subscription-id";
// The application id of the service principal to be used to authenticate.
string clientId = "my-client-id";
// The client secret created along with the service principal used for authentication.
// You might want to look at Arcus Security to ensure this value can be stored in Azure Key Vault instead
string clientSecret = "my-client-secret";

// Use the LogicAppAuthentication-class to retrieve the required access token.
var authentication = LogicAppAuthentication.UsingServicePrincipal(tenantId, subscriptionId, clientId, clientSecret);
```

*Read more about how to authenticate and gain access to your Logic Apps in [our documentation](https://invictus-integration.github.io/testing-framework/#/logic-apps/authentication).*  

### Controlling an Azure Logic App

One of the key aspects of our framework is the ability to have control over every single Azure Logic App that is part of your interface. This can be done by only specifying the name of the resource group and the Azure Logic App itself.   

Once authenticated, you can perform any of the following operations:  
- Get the Logic App metadata  
- Get the Logic App trigger-URL  
- (Temporarily) enable the Logic App  
- (Temporarily) update the Logic App definition  
- (Temporarily) enable static results on specific actions within the Logic App  
- Trigger a Logic App run  
- Cancel a Logic App run  
- Delete a Logic App  

All of these features provide you the possibility to adjust and trigger your Azure Logic Apps to allow for a specific scenario to be tested.  

As you don't want your unit/integration test related Logic App to inflict costs while not being used, here is an example of how you can temporarily enable an Azure Logic App during your test:  
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

*Read more about how to control an Azure Logic App in [our documentation](https://invictus-integration.github.io/testing-framework/#/logic-apps/control-single-logicapp).*  

### Monitoring an Azure Logic App

After preparing & triggering the interface to match your test scenario, it is important to be able to retrieve enough information to verify the outcome of the test.  
To accommodate this, we provide a fluent way of describing what you are looking for allowing you to:  
- Get an Azure Logic App run by its correlation ID (aka *client tracking ID*)
- Get an Azure Logic App run by one or more tracked properties
- Get an Azure Logic App run within a specific time frame  
- Get a list of Azure Logic App runs within a specific time frame
- Get a given number of completed Azure Logic App runs (polling).   
  *In case you have the same Logic App running multiple times in parallel.* 

Every Azure Logic App run provides information about all metadata of the run itself such the status, what triggered it, start/end time, correlation, information & tracked properties for every single action and more!  

To give you an idea of how you can use our framework to monitor Logic App runs, let's take a look!  
In this first example, we will be polling for a single run that contains the specified correlation id and give up after 15 seconds:
```csharp
LogicAppRun logicAppRun =
    await LogicAppsProvider.LocatedAt(resourceGroup, logicAppName, authentication)
                           .WithTimeout(TimeSpan.FromSeconds(15))
                           .WithCorrelationId("08586073923413753771945113291CU110")
                           .PollForSingleLogicAppRunAsync();
```
If the framework was unable to find a run within the specific time frame, a `TimeOutException` will be thrown.

In this last example, we are expecting to find 3 Azure Logic Apps runs who have _OrderNumber_ as a tracked property where we expect `123456` as its value.  
To achieve this, we will be checking for 60 seconds and move on if we couldn't find them.  
```csharp
IEnumerable<LogicAppRun> logicAppRuns =
    await LogicAppsProvider.LocatedAt(resourceGroup, logicAppName, authentication)
                           .WithTimeout(TimeSpan.FromSeconds(60))
                           .WithTrackedProperty("OrderNumber", "123456")
                           .PollForLogicAppRunsAsync(minimumNumberOfItems: 3);
```
As was the case when looking for a single run, a `TimeOutException` will be thrown if the framework was unable to find the requested number of runs within the allotted time frame.


*Read more about how to monitor an Azure Logic App in [our documentation](https://invictus-integration.github.io/testing-framework/#/logic-apps/polling-logicapp-runs).*  


# Conclusion
We are happy to help the Azure Logic Apps community to build automated test suits on top of Azure Logic Apps! We welcome everybody to add feature requests [on GitHub](https://github.com/invictus-integration/testing-framework/) and accept contributions to make the framework even better!
