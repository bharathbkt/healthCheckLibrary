# Health Monitoring Module

A comprehensive ABP.io Commercial-compatible health monitoring module for .NET 6, featuring standard infrastructure checks (Redis, MongoDB, Kafka) and a deep DB-First Oracle Schema Validation check.

## Architecture Context

* **Framework**: .NET 6 Class Library targeting the ABP framework.
* **Paradigm**: Dependency Injection pattern implemented natively using `ITransientDependency` and the generic ABP `AbpModule`.
* **Database Target**: Oracle Database accessed via EF Core 6 mapped contexts.

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

### Program.cs Integration Snippet

When integrating this module into your ABP Web Host or API layer, you must map the health check endpoints appropriately. It is best practice to separate simple standard probes (liveness) from deep semantic checks.

Below is a snippet for your `Program.cs` file showing how to register a `/health/startup` route filtered specifically for the `deep` tags to prevent blocking rapid liveness probes:

```csharp
app.UseEndpoints(endpoints =>
{
    // Liveness probe
    endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false
    });

    // Readiness probe for dependencies like Kafka, Redis, Oracle DB, MongoDB, and filepath access
    endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        // Filter checks by tags added during registration
        Predicate = check => check.Tags.Contains("infrastructure") || 
                             check.Tags.Contains("database") || 
                             check.Tags.Contains("filepath")
    });

    // Startup probe (e.g. for deep schema validation)
    endpoints.MapHealthChecks("/health/startup", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("deep"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
});
```

To bind the generic schema checking class onto your respective `MyOracleDbContext`, simply execute a call akin to following in an adjacent module, or add it to `HealthMonitoringAbpModule`:

```csharp
context.Services.AddHealthChecks()
       .AddCheck<OracleSchemaValidationHealthCheck<MyOracleDbContext>>(
           name: "Oracle-Schema-Deep-Check",
           failureStatus: HealthStatus.Degraded,
           tags: new[] { "deep", "oracle", "database" }
       );
```
