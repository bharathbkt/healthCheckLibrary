using System;

namespace HealthMonitoringModule;

public class HealthMonitoringOptions
{
    public string RedisConnectionStringKey { get; set; } = "ConnectionStrings:Redis";
    public System.Collections.Generic.List<string> MongoDbConnectionStringKeys { get; set; } = new System.Collections.Generic.List<string> { "ConnectionStrings:MongoDb" };
    public System.Collections.Generic.List<string> OracleConnectionStringKeys { get; set; } = new System.Collections.Generic.List<string> { "ConnectionStrings:Oracle" };
    public string KafkaBootstrapServersKey { get; set; } = "Kafka:BootstrapServers";
    public string FilePathKey { get; set; } = "HealthChecks:FilePath";
    public string TimeoutSecondsKey { get; set; } = "HealthChecks:TimeoutSeconds";
}
