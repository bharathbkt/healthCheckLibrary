using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Microsoft.Extensions.Configuration;

namespace HealthMonitoringModule;

public class HealthMonitoringAbpModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        context.Services.AddHealthChecks()
            // 1. Redis
            .AddRedis(
                redisConnectionString: configuration.GetConnectionString("Redis") ?? "localhost:6379",
                name: "Redis-Check",
                tags: new[] { "infrastructure", "redis" })
            // 2. MongoDB
            .AddMongoDb(
                mongodbConnectionString: configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017",
                name: "MongoDb-Check",
                tags: new[] { "infrastructure", "mongodb"})
            // 3. Kafka
            .AddKafka(
                setup: options => 
                {
                    options.BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
                },
                name: "Kafka-Check",
                tags: new[] { "infrastructure", "kafka" })
            // 4. Filepath Access Check
            .AddCheck("FilePath-Check", () => 
            {
                var testPath = configuration["HealthChecks:FilePath"] ?? System.IO.Path.GetTempPath();
                try
                {
                    // Verify read/write access to the specific file path
                    var testFile = System.IO.Path.Combine(testPath, "healthcheck_test.tmp");
                    System.IO.File.WriteAllText(testFile, "access_test");
                    System.IO.File.Delete(testFile);
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Read/Write access to {testPath} successful.");
                }
                catch (System.Exception ex)
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"Failed to access directory {testPath}.", ex);
                }
            }, tags: new[] { "infrastructure", "filepath" });
                
        // Note: The Deep Oracle Schema Validation (DB-First) Health Check 
        // typically needs to be registered here targeting an specific EF Core DbContext:
        //
        // context.Services.AddHealthChecks()
        //      .AddCheck<OracleSchemaValidationHealthCheck<YourOracleDbContext>>(
        //          name: "Oracle-Schema-Deep-Check",
        //          failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        //          tags: new[] { "deep", "oracle", "database" });
    }
}
