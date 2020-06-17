[home](../README.md)

# Polling Azure Logic App runs

The `Invictus.Testing` library provides a way to retrieve Logic App runs during integration testing.

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

var authentication = LogicAppAuthentication.UsingServicePrincipal(tenantId, subscriptionId, clientId, clientSecret);
var provider = LogicAppsProvider.LocatedAt(resourceGroupName, logicAppName, authentication);
```

### Filtering features

These features describe how to filter for the correct Logic App runs.

**Start time**

The library allows you to set a time when the Logic App runs should be started.

```csharp
LogicAppsProvider provider =
    LogicAppsProvider.LocatedAt(...)
                     .WithStartTime(DateTimeOffset.UtcNow);
```

**Timeout**

The library allows you to set a timeout period in which the polling should happen.

```csharp
LogicAppsProvider provider =
    LogicAppsProvider.LocatedAt(...)
                     .WithTimeout(TimeSpan.FromSeconds(90));
```

**Correlation ID**

The library allows you to set a correlation ID a.k.a 'client tracking ID' to filter for Logic Apps with the same ID.

```csharp
LogicAppsProvider provider =
    LogicAppsProvider.LocatedAt(...)
                     .WithCorrelationId("client-tracking-ID");
```

**Tracking properties**

The library allows you to set multiple tracking properties to filter for Logic Apps with the same tracking ID's.

```csharp
LogicAppsProvider provider =
    LogicAppsProvider.LocatedAt(...)
                     .WithTrackedProperty("tracked-property-name-1", "some value")
                     .WithTrackedProperty("tracked-property-name-2", "some other value");
```

### Number of expected items features

These features describe how the filtering should translate into a set of Logic App runs.

**get as many run during the timeout period**

The library allows you to poll in a given time(out) frame for all the Logic App runs that match the filtering criteria.

This example will poll for 30 seconds and collect all Logic App runs that has a correlation ID that matches `"client-tracking-ID`.

```csharp
IEnumerable<LogicAppRun> logicAppRuns =
    await LogicAppsProvider.LocatedAt(...)
                           .WithTimeout(TimeSpan.FromSeconds(30))
                           .WithCorrelationId("client-tracking-ID")
                           .PollForLogicAppRunsAsync();
```

**get a single item**

The library allows you to poll for a single Logic App run that matches the filtering criteria.

This example will poll for 10 seconds and collect the first Logic App run that has a tracking property that matches `["tracked-property"] = "some value"`.

```csharp
LogicAppRun logicAppRun =
    await LogicAppsProvider.LocatedAt(...)
                           .WithTimeout(TimeSpan.FromSeconds(10))
                           .WithTrackedProperty("tracked-property", "some value")
                           .PollForSingleLogicAppRunAsync();
```

**get a minimum number of items**

The library allows you to poll for a minimum amount of Logic App runs that match the filtering criteria.

This example will poll for 90 seconds and tries to collect 5 Logic App runs that were started at the given start time.

```csharp
IEnumerable<LogicAppRun> logicAppRuns =
    await LogicAppsProvider.LocatedAt(...)
                           .WithTimeout(TimeSpan.FromSeconds(90))
                           .WithStartTime(DateTimeOffset.UtcNow)
                           .PollForLogicAppRunsAsync(minimumNumberOfItems: 5);
```