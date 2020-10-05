[home](../README.md)

# Control a single Azure Logic App

The `Invictus.Testing.LogicApps` library provides a way to control a single Logic App during integration testing.

## Installation

The features described here requires the following package:

```shell
> Install-Package Invictus.Testing.LogicApps
```

## Features

All the following features uses the `LogicAppClient` which can be created using [an Logic App authentication](/authentication.md).

### Gets Logic App metadata

This library allows you to retrieve extra metadata about the logic app, such as the name, the current workflow definition, access endpoint...

```csharp
using (var logicApp = ...)
{
    LogicAppMetadata metadata = logicApp.GetMetadataAsync();
    string name = metadata.Name;
    string accessEndpoint = metadata.AccessEndpoint;
    ...
}
```

### Gets Logic App trigger URL

The library allows you to retrieve the trigger URL on which the Logic App can by run.
This can be done by either specifying for which trigger you want the URL or by leaving the testing framework to find the configured trigger.

```csharp
using (var logicApp = ...)
{
    // Get trigger URL by trigger name.
    string triggerUrl = await logicApp.GetTriggerUrlByNameAsync("my-trigger");

    // Get trigger URL of first found trigger.
    string triggerUrl = await logicApp.GetTriggerUrlAsync();
}
```

### Temporary enable/disable Logic App

The library allows you to temporary enable or disable a Logic App which makes sure that after the test the Logic App is back to its disabled state.

**Enabling**

```csharp
using (var logicApp = ...)
{
    await using (await logicApp.TemporaryEnableAsync())
    {
    }
}
```

The Logic App will be disabled when the returned disposable gets disposed.

**Disabling**

```csharp
using (var logicApp = ...)
{
    await using (await logicApp.TemporaryDisableAsync())
    {
    }
}
```

The Logic App will be enabled when the returned disposable gets disposed.

### Temporary update Logic App's workflow definition

This library allows you to temporary update the Logic App's workflow definition which makes sure that after the test the original workflow definition is restored.

```csharp
string workflowDefinition = ...
using (var logicApp = ...)
{
    await using (await logicApp.TempraryUpdateAsync(workflowDefinition))
    {
    }
}
```

The original Logic App's workflow definition will be restored when the returned disposable gets disposed.

### Temporary enabling static result on Logic App

This library allows you to temporary enable a static result for a given Logic App action.

This can be done in three different ways:
* Temporary enable a successful result (`Status = OK`, `StatusCode = `OK`) for a single Logic App action.

```csharp
using (var logicApp = ...)
{
    await using (await logicApp.TemporaryEnableSuccessStaticResultAsync("my-action"))
    {
    }
}
```

* Temporary enable a custom static result for a single Logic App action.

```csharp
using (var logicApp = ...)
{
    var definition = new StaticResultDefinition
    {
        Status = "Succeeded",
        Outputs =  new Outputs { StatusCode = "OK" }
    };

    await using (await logicApp.TemporaryEnableStaticResultAsync("my-action", ))
    {
    }
}
```

* Temporary enable a series of custom static results for a corresponding set of Logic App actions. 

```csharp
using (var logicApp = ...)
{
    var definition = new StaticResultDefinition
    {
        Status = "Succeeded",
        Outputs =  new Outputs { StatusCode = "OK" }
    };

    var definitions = new Dictionary<string, StaticResultDefinition>
    {
        ["my-action"] = definition
    };

    await using (await logicApp.TemporaryEnableStaticResultsAsync(definitions))
    {
    }
}
```

The static result(s) will be removed once the returned disposable gets disposed.

### Running an Logic App

This library allows you to run the Logic App by specifiying a trigger name or by leave it up to the framework to find the first configured trigger on the Logic App.

```csharp
using (var logicApp = ...)
{
    // Run Logic App by trigger name.
    await logicApp.RunByNameAsync("my-trigger-name");

    // Run Logic App of first found trigger.
    await logicApp.RunAsync();
}
```

### Triggering an Logic App

This library allows you to trigger an Logic App by posing headers on a trigger URL.

```csharp
using (var logicApp = ...)
{
    var headers = new Dictionary<sring, string>
    {
        ["TestHeader"] = "TestValue"
    };

    logicApp.TriggerAsync(headers);
}
```

### Delete an Logic App

This library allows you to delete the Logic App entirely.

```csharp
using (var logicApp = ...)
{
    await logicApp.DeleteAsync();
}
```
