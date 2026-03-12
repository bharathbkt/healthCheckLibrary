using System;

namespace HealthMonitoringModule;

public class HealthMonitoringOptions
{
    public string RedisConnectionStringKey { get; set; } = "ConnectionStrings:Redis";
    public string MongoDbConnectionStringKey { get; set; } = "ConnectionStrings:MongoDb";
    public string OracleConnectionStringKey { get; set; } = "ConnectionStrings:Oracle";
    public string KafkaBootstrapServersKey { get; set; } = "Kafka:BootstrapServers";
    public string FilePathKey { get; set; } = "HealthChecks:FilePath";
    public string TimeoutSecondsKey { get; set; } = "HealthChecks:TimeoutSeconds";
}
