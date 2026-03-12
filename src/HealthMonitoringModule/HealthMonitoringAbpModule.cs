using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Microsoft.Extensions.Configuration;

namespace HealthMonitoringModule;

public class HealthMonitoringAbpModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // Bind settings from HealthMonitoring section
        var options = new HealthMonitoringOptions();
        configuration.GetSection("HealthMonitoring").Bind(options);

        // Make options available in DI for future use
        context.Services.Configure<HealthMonitoringOptions>(configuration.GetSection("HealthMonitoring"));

        var timeoutSeconds = configuration.GetValue<int>(options.TimeoutSecondsKey, 3);
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        context.Services.AddHealthChecks()
            // 1. Redis
            .AddRedis(
                redisConnectionString: configuration.GetConnectionString(options.RedisConnectionName) ?? configuration[options.RedisConnectionName]!,
                name: "Redis-Check",
                tags: new[] { "infrastructure", "redis" },
                timeout: timeout)
            // 2. MongoDB
            .AddMongoDb(
                mongodbConnectionString: configuration.GetConnectionString(options.MongoDbConnectionName) ?? configuration[options.MongoDbConnectionName]!,
                name: "MongoDb-Check",
                tags: new[] { "infrastructure", "mongodb"},
                timeout: timeout)
            // 3. Kafka
            .AddKafka(
                setup: kafkaOptions => 
                {
                    kafkaOptions.BootstrapServers = configuration[options.KafkaBootstrapServersKey]!;
                },
                name: "Kafka-Check",
                tags: new[] { "infrastructure", "kafka" },
                timeout: timeout)
            // 4. Filepath Access Check
            .AddCheck("FilePath-Check", () => 
            {
                // Rely strictly on configuration
                var testPath = configuration[options.FilePathKey];
                if (string.IsNullOrEmpty(testPath)) 
                {
                    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy($"{options.FilePathKey} configuration is missing.");
                }

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
            }, tags: new[] { "infrastructure", "filepath" }, timeout: timeout)
            // 5. Oracle DB Check (Basic / Readiness)
            .AddOracle(
                configuration.GetConnectionString(options.OracleConnectionName) ?? configuration[options.OracleConnectionName]!,
                name: "Oracle-Basic-Check",
                tags: new[] { "infrastructure", "database", "oracle" },
                timeout: timeout);
                
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
