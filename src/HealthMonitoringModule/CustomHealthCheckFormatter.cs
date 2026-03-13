using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HealthMonitoringModule;

public static class CustomHealthCheckFormatter
{
    public static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        var options = context.RequestServices.GetService<IOptions<HealthMonitoringOptions>>()?.Value;
        var configuration = context.RequestServices.GetService<IConfiguration>();

        context.Response.ContentType = "application/json; charset=utf-8";

        var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

        var response = new
        {
            status = healthReport.Status.ToString(),
            totalDuration = healthReport.TotalDuration.ToString(),
            entries = healthReport.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    timeTaken = e.Value.Duration.ToString(), // "timetaken"
                    resourceDetails = GetResourceDetails(e.Key, options, configuration), // "resource details"
                    description = e.Value.Description,
                    data = e.Value.Data,
                    error = e.Value.Exception?.Message,
                    tags = e.Value.Tags
                }
            )
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, jsonOptions));
    }

    private static string? GetResourceDetails(string checkName, HealthMonitoringOptions? options, IConfiguration? configuration)
    {
        if (options == null || configuration == null) return null;

        return checkName switch
        {
            "Redis-Check" => configuration[options.RedisConnectionStringKey],
            "MongoDb-Check" => configuration[options.MongoDbConnectionStringKey],
            "Kafka-Check" => configuration[options.KafkaBootstrapServersKey],
            "Oracle-Basic-Check" => configuration[options.OracleConnectionStringKey],
            "FilePath-Check" => configuration[options.FilePathKey],
            "Oracle-Schema-Deep-Check" => configuration[options.OracleConnectionStringKey],
            _ => null
        };
    }
}
