using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;
using Microsoft.Extensions.Configuration;

namespace HealthMonitoringModule;

public class HealthMonitoringAbpModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        // Execute pre-configured actions from dependent modules
        var options = context.Services.ExecutePreConfiguredActions<HealthMonitoringOptions>();

        // Make options available in DI for future use
        context.Services.Configure<HealthMonitoringOptions>(opt => 
        {
            opt.RedisConnectionStringKey = options.RedisConnectionStringKey;
            opt.MongoDbConnectionStringKey = options.MongoDbConnectionStringKey;
            opt.OracleConnectionStringKey = options.OracleConnectionStringKey;
            opt.KafkaBootstrapServersKey = options.KafkaBootstrapServersKey;
            opt.FilePathKey = options.FilePathKey;
            opt.TimeoutSecondsKey = options.TimeoutSecondsKey;
        });

        var timeoutSeconds = configuration.GetValue<int>(options.TimeoutSecondsKey, 3);
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var healthChecksBuilder = context.Services.AddHealthChecks();

        // 1. Redis
        var redisConnectionString = configuration[options.RedisConnectionStringKey];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString: redisConnectionString,
                name: "Redis-Check",
                tags: new[] { "infrastructure", "redis" },
                timeout: timeout);
        }

        // 2. MongoDB
        var mongoDbConnectionString = configuration[options.MongoDbConnectionStringKey];
        if (!string.IsNullOrWhiteSpace(mongoDbConnectionString))
        {
            healthChecksBuilder.AddMongoDb(
                mongodbConnectionString: mongoDbConnectionString,
                name: "MongoDb-Check",
                tags: new[] { "infrastructure", "mongodb"},
                timeout: timeout);
        }

        // 3. Kafka
        var kafkaBootstrapServers = configuration[options.KafkaBootstrapServersKey];
        if (!string.IsNullOrWhiteSpace(kafkaBootstrapServers))
        {
            healthChecksBuilder.AddKafka(
                setup: kafkaOptions => 
                {
                    kafkaOptions.BootstrapServers = kafkaBootstrapServers;
                },
                name: "Kafka-Check",
                tags: new[] { "infrastructure", "kafka" },
                timeout: timeout);
        }

        // 4. Filepath Access Check
        var filePath = configuration[options.FilePathKey];
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            healthChecksBuilder.AddCheck("FilePath-Check", () => 
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
            }, tags: new[] { "infrastructure", "filepath" }, timeout: timeout);
        }

        // 5. Oracle DB Check (Basic / Readiness)
        var oracleConnectionString = configuration[options.OracleConnectionStringKey];
        if (!string.IsNullOrWhiteSpace(oracleConnectionString))
        {
            healthChecksBuilder.AddOracle(
                oracleConnectionString,
                name: "Oracle-Basic-Check",
                tags: new[] { "infrastructure", "database", "oracle" },
                timeout: timeout);
        }
                
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
