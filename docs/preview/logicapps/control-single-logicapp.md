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
string subscriptionId = "my-subscription-id";
string resourceGroup = "my-resource-group";
string logicAppName = "my-logic-app-name";

var authentication = LogicAppAuthentication.Create();
using (var logicApp = await LogicAppClient.CreateAsync(resourceGroup, logicAppName, authentication))
{
}
```

### 
