# Health Check POC Implementation Documentation

## Overview
This document outlines the detailed implementation and usage of the `HealthMonitoringModule` within the `HealthCheckPOC` application. The module provides a standardized, Abp framework-compatible way to plug in various health checks for infrastructure dependencies (Redis, MongoDB, Kafka, File systems, and Oracle Database). 
It exposes standard health endpoints for **Liveness**, **Readiness**, and **Startup/Deep** probes.

## Architecture

The health check implementation is separated into a reusable ABP module `HealthMonitoringAbpModule` which can be depended upon by consuming projects (e.g., `HealthCheckPOC.HttpApi.Host`). This allows the health checks to be centralized, consistent, and easily configurable.

### Key Components
1. **`HealthMonitoringAbpModule`**: The core ABP module that registers health checks during the service configuration phase based on the provided configuration variables.
2. **`HealthMonitoringOptions`**: An options class mapping dependency connection settings configurable via keys from `appsettings.json`.
3. **`CustomHealthCheckFormatter`**: A utility class to intercept and format the built-in Microsoft health check report into a standardized JSON response containing detailed execution metrics, durations, and dynamically injected connection details.
4. **`OracleSchemaValidationHealthCheck<TDbContext>`**: A specialized health check designed to perform a deep validation of the Entity Framework Core model against the actual remote Oracle database schema.

---

## Registered Probe Endpoints

The HTTP API host (`HealthCheckPOCHttpApiHostModule`) wires routing to expose three specialized health probe endpoints.

* **`/health/live` (Liveness)**
  - **Purpose**: Indicates if the application process is running and can handle HTTP requests.
  - **Checks**: Self-check, no external dependencies are verified.

* **`/health/ready` (Readiness)**
  - **Purpose**: Checks if the application is ready to process traffic. Failing this probe will remove the instance from the load balancer rotation in Kubernetes/Cloud environments.
  - **Checks**: Verifies basic connectivity to infrastructure dependencies (Redis, MongoDB, Kafka, basic Oracle DB connection, and Directory/FilePath read/write access).

* **`/health/startup` (Startup / Deep Probe)**
  - **Purpose**: Validates critical data structures or lengthy initializations. Can be used in slow-starting containers to avoid being killed prematurely.
  - **Checks**: Runs `OracleSchemaValidationHealthCheck`, which inspects table lengths, column existence, and datatype configurations.

---

## Detailed Health Check Operations

The `HealthMonitoringAbpModule` registers various dependencies based on checking if their mapped connection strings have a non-empty value in the configuration. 

### 1. Redis Check (`Redis-Check`)
- **Type**: Infrastructure
- **Purpose**: Verifies connection to the configured Redis instance.
- **Config Key Mapped**: Extracted via `options.RedisConnectionStringKey` (Default: `ConnectionStrings:Redis`, Overridden in Host to `appsettings:redisconnection`).
- **Condition**: Registered only if the connection string is populated.

### 2. MongoDB Check (`MongoDb-Check`)
- **Type**: Infrastructure
- **Purpose**: Validates connection to the MongoDB cluster/node.
- **Config Key Mapped**: Extracted via `options.MongoDbConnectionStringKey` (Overridden in Host to `dbSettings:mongodbconnection`).
- **Condition**: Registered only if the connection string is populated.

### 3. Kafka Check (`Kafka-Check`)
- **Type**: Infrastructure
- **Purpose**: Validates connectivity to Kafka brokers.
- **Config Key Mapped**: `Kafka:BootstrapServers`

### 4. File Path Access Check (`FilePath-Check`)
- **Type**: Infrastructure / Filepath
- **Purpose**: Verifies that the application process has correct Write/Read access to a specifically mapped physical path by creating, writing to, and deleting a `.tmp` file.
- **Config Key Mapped**: `HealthChecks:FilePath`

### 5. Oracle Database Checks
Oracle databases are validated on multiple tiers:

#### A. Basic Oracle Check (`Oracle-Basic-Check`)
- **Type**: Infrastructure
- **Purpose**: Basic ping over ADO.NET to verify the server is reachable and credentials are valid.
- **Config Key Mapped**: Extracted via `options.OracleConnectionStringKey`.

#### B. Oracle Schema Deep Validation (`Oracle-Schema-Deep-Check`)
- **Type**: Deep
- **How it works**: Uses exact schema information derived from `IModel` loaded into an Entity Framework `DbContext`.
- Uses `ALL_TAB_COLUMNS` in the Oracle metadata catalogs.
- Identifies if application entities exist as physical tables.
- Cross-references Entity Framework property attributes (`MaxLength`, `DataType`) against the physical database structure's `CHAR_LENGTH`, `DATA_LENGTH`, and `DATA_TYPE`.
- If differences are found, it degrades the health status and outlines each mismatch in the response data.

---

## Custom Health Check Formatter JSON Output

A robust, descriptive JSON formatter (`CustomHealthCheckFormatter`) converts the diagnostic data into an APM-friendly output format. 

**An example payload:**
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0450000",
  "entries": {
    "Redis-Check": {
      "status": "Healthy",
      "timeTaken": "00:00:00.0120000",
      "resourceDetails": "localhost:6379",
      "description": "Redis check details",
      "data": {},
      "error": null,
      "tags": ["infrastructure", "redis"]
    }
  }
}
```

The formatter enriches the output with a `resourceDetails` field which extracts the associated configuration string value at runtime based on the `checkName` (E.g. showing exactly what Redis endpoint is being hit).

---

## Configuration Setup Guide

When consuming the `HealthMonitoringModule` inside a downstream ABP application, perform the following:

### 1. Import the Module
Add `typeof(HealthMonitoringAbpModule)` to your application's `[DependsOn(...)]` list in the host module.

### 2. Adjust Option Maps (Host `PreConfigureServices`)
Map which keys in your `appsettings.json` correlate to which infrastructure checks:

```csharp
public override void PreConfigureServices(ServiceConfigurationContext context)
{
    PreConfigure<HealthMonitoringOptions>(options =>
    {
        // Example configuration override:
        options.MongoDbConnectionStringKey = "dbSettings:mongodbconnection";
        options.TimeoutSecondsKey = "HealthChecks:TimeoutSeconds";
    });
}
```

### 3. Add Custom/Deep Checks (Host `ConfigureServices`)
Since `OracleSchemaValidationHealthCheck` relies on a generic `<TDbContext>`, define it specifically for your app's `DbContext`:

```csharp
context.Services.AddHealthChecks()
    .AddCheck<OracleSchemaValidationHealthCheck<MyCustomAppDbContext>>(
        name: "Oracle-Schema-Deep-Check",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "deep", "oracle" },
        timeout: TimeSpan.FromSeconds(3));
```

### 4. Provide the App Settings (`appsettings.json`)
The application requires the specified mapped keys to exist.

```json
{
  "appsettings": {
    "redisconnection": "localhost:6379"
  },
  "dbSettings": {
    "mongodbconnection": "mongodb://localhost:27017"
  },
  "HealthChecks": {
    "FilePath": "C:\\temp\\healthcheck",
    "TimeoutSeconds": 3
  }
}
```
*Note: Any check whose configuration value is blank/missing gracefully skips registration.*
