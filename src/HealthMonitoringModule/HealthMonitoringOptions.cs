using System;

namespace HealthMonitoringModule;

public class HealthMonitoringOptions
{
    public string RedisConnectionName { get; set; } = "Redis";
    public string MongoDbConnectionName { get; set; } = "MongoDb";
    public string OracleConnectionName { get; set; } = "Oracle";
    public string KafkaBootstrapServersKey { get; set; } = "Kafka:BootstrapServers";
    public string FilePathKey { get; set; } = "HealthChecks:FilePath";
    public string TimeoutSecondsKey { get; set; } = "HealthChecks:TimeoutSeconds";
}
