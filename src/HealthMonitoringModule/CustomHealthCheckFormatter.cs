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

        if (checkName == "Redis-Check") return configuration[options.RedisConnectionStringKey];
        if (checkName == "Kafka-Check") return configuration[options.KafkaBootstrapServersKey];
        if (checkName == "FilePath-Check") return configuration[options.FilePathKey];
        
        if (checkName == "Oracle-Schema-Deep-Check") 
        {
             return options.OracleConnectionStringKeys != null && options.OracleConnectionStringKeys.Count > 0 
                ? configuration[options.OracleConnectionStringKeys[0]] 
                : null;
        }

        if (checkName.StartsWith("MongoDb-Check-"))
        {
            if (int.TryParse(checkName.Replace("MongoDb-Check-", ""), out int index) && options.MongoDbConnectionStringKeys != null && index >= 1 && index <= options.MongoDbConnectionStringKeys.Count)
            {
                return configuration[options.MongoDbConnectionStringKeys[index - 1]];
            }
            return null;
        }

        if (checkName.StartsWith("Oracle-Basic-Check-"))
        {
            if (int.TryParse(checkName.Replace("Oracle-Basic-Check-", ""), out int index) && options.OracleConnectionStringKeys != null && index >= 1 && index <= options.OracleConnectionStringKeys.Count)
            {
                return configuration[options.OracleConnectionStringKeys[index - 1]];
            }
            return null;
        }

        return null;
    }
}
