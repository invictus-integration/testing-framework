[home](../index.md) | [logic apps](logicapps.md)

# Control a single Azure Logic App

The `Invictus.Testing` library provides a way to control a single Logic App during integration testing.

## Installation

The features described here requires the following package:

```shell
> Install-Package Invictus.Testing
```

## Features

All the following features uses the `LogicAppClient` which can be created using:

```csharp
string tenantId = "my-tenant-id";
string subscriptionId = "my-subscription-id";
string clientId = "my-client-id";
string clientSecret = "my-client-secret";
string resourceGroup = "my-resource-group";
string logicAppName = "my-logic-app-name";

var authentication = LogicAppAuthentication.UsingServicePrincipal();
using (var logicApp = await LogicAppClient.CreateAsync(resourceGroup, logicAppName, authentication))
{
}
```

### Temporary enable Logic App

The library allows you to temporary enable a Logic App which makes sure that after the test the Logic App is back to its disabled state.

```csharp
using (var logicApp = ...)
{
    await using (await logicApp.TemporaryEnableAsync())
    {
    }
}
```

The Logic App will be disabled when the returned disposable gets disposed.
