# Health Monitoring Module

A comprehensive ABP.io Commercial-compatible health monitoring module for .NET, featuring standard infrastructure checks (Redis, MongoDB, Kafka, File systems) and a deep DB-First Oracle Schema Validation check. It supports robust dynamic configuration mappings, allowing consuming applications to define their own connection string configuration keys.

## Architecture Context

* **Framework**: .NET Class Library targeting the ABP framework.
* **Paradigm**: Dependency Injection pattern implemented natively using `ITransientDependency` and the generic ABP `AbpModule`. 
* **Database Target**: Oracle Database accessed via EF Core mapped contexts.

## Key Features

1. **Dynamic Configuration via `HealthMonitoringOptions`**: Consumers can dynamically assign which `appsettings.json` keys map to which health check by configuring options in the `PreConfigureServices` phase.
2. **Standardized JSON Formatter**: Utilizes a `CustomHealthCheckFormatter` to format Microsoft's built-in health reports into a clean JSON structure. It additionally injects the specific evaluated `resourceDetails` (such as the actual connection endpoints or file paths from `IConfiguration`) directly into the API response.
3. **Deep Oracle Validation**: Performs schema comparisons between the Entity Framework Core model layer and Oracle's metadata catalogs (`ALL_TAB_COLUMNS`).

## Security & Configuration

### Oracle Database User Permissions

To perform the deep schema validation via the system view `ALL_TAB_COLUMNS`, the database user configuring the EF Core connection must possess the appropriate visibility privileges over table definitions.

**Requirement**: The Oracle DB User requires the `SELECT_CATALOG_ROLE` or explicit `SELECT` permissions on `ALL_TAB_COLUMNS`.

```sql
-- Granting explicit access to a user
GRANT SELECT ON ALL_TAB_COLUMNS TO YOUR_DB_USER;

-- Alternatively: granting the catalog role
GRANT SELECT_CATALOG_ROLE TO YOUR_DB_USER;
```

If the `DbUser` lacks these permissions, the Oracle Schema Health Check will gracefully degrade, logging a mismatch message identifying the missing `ALL_TAB_COLUMNS` access.

### Host Module Integration

When integrating this module into your ABP API or Host Application, you must map the configuration options and then map the health check endpoints appropriately with our custom JSON formatter.

#### 1. Configure the module options (`PreConfigureServices`)

In your generic Host Module (e.g. `HealthCheckPOCHttpApiHostModule`), adjust the predefined option keys to align with the connection keys present in your `appsettings.json` file.

```csharp
public override void PreConfigureServices(ServiceConfigurationContext context)
{
    PreConfigure<HealthMonitoringOptions>(options =>
    {
        // Example overriding the expected MongoDB setting key path:
        options.MongoDbConnectionStringKey = "dbSettings:mongodbconnection";
        
        // Uncomment to override the expected Oracle key:
        // options.OracleConnectionStringKey = "dbSettings:oracleconnection";
    });
}
```

#### 2. Register application-specific Deep Checks (`ConfigureServices`)

Bind the generic deep schema checking class onto your application's respective `DbContext`.

```csharp
var timeout = TimeSpan.FromSeconds(3);

context.Services.AddHealthChecks()
    .AddCheck<OracleSchemaValidationHealthCheck<MyAppDbContext>>(
        name: "Oracle-Schema-Deep-Check",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "deep", "oracle" },
        timeout: timeout
    );
```

#### 3. Map Endpoints with the Custom Formatter (`OnApplicationInitialization`)

Register the `/health/live`, `/health/ready`, and `/health/startup` routes utilizing the `CustomHealthCheckFormatter.WriteResponse` to return standard, verbose JSON.

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();

    // 1. Liveness Probe (immediate response, no dependency checks)
    endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
        ResponseWriter = CustomHealthCheckFormatter.WriteResponse
    });

    // 2. Readiness Probe (checks Kafka, Redis, MongoDB, Filepath Access)
    endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("infrastructure") || 
                             check.Tags.Contains("database") || 
                             check.Tags.Contains("filepath"),
        ResponseWriter = CustomHealthCheckFormatter.WriteResponse
    });

    // 3. Startup/Deep Probe (Oracle Schema validation)
    endpoints.MapHealthChecks("/health/startup", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("deep"),
        ResponseWriter = CustomHealthCheckFormatter.WriteResponse
    });
});
```

## Documentation Reference

For a more comprehensive breakdown of the inner workings, probe semantics, dynamic endpoint binding, and example JSON response formatting, refer directly to [HealthCheckDocumentation.md](./HealthCheckDocumentation.md) in this repository.
