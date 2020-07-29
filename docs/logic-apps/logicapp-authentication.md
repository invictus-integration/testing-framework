[home](../README.md)

# Authenticating to Azure Logic App resource

When using the Logic App testing features, you require a valid authentication setup.
Following shows what is required in both cases:

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