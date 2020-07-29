[home](../README.md)

# Authenticating to Azure Logic App resource

Before you can start using the Azure Logic App testing features, you need to authenticate with Microsoft Azure.

As of today, we provide the following authentication scenarios:

- [**Using Service Principal**](#using-a-service-principal)

## Using a Service Principal

Before we can authenticate, you'll need to [create an Azure AD application](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal) which will be used as your service principle.

The service principal will need to have at least [`Logic App Contributor`](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles#logic-app-contributor) permissions on all your Azure Logic App instances that you want to test.

When using a service principal, you need to provide the following information:
- **Tenant Id** - Identifier of the Azure AD directory
- **Subscription Id** - Identifier of the Azure subscription that contains your Azure Logic App
- **Client Id** - Identifier of the service principal in Azure AD. In this case, it is the application id of the Azure AD app.
- **Client Id** - Secret of the service principal in Azure AD to authenticate with.


```csharp
string tenantId = "my-tenant-id";
string subscriptionId = "my-subscription-id";
string clientId = "my-client-id";
string clientSecret = "my-client-secret";
string resourceGroup = "my-resource-group";
string logicAppName = "my-logic-app-name";

var authentication = LogicAppAuthentication.UsingServicePrincipal(tenantId, subscriptionId, clientId, clientSecret);

// For the Logic App provider
var provider = LogicAppsProvider.LocatedAt(resourceGroupName, logicAppName, authentication);

// For the Logic App client
using (var logicApp = await LogicAppClient.CreateAsync(resourceGroup, logicAppName, authentication))	
{	
}
```
