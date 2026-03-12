using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace HealthCheckPOC.HttpApi.Host;

public static class CustomHealthCheckFormatter
{
    public static Task WriteResponse(HttpContext context, HealthReport healthReport)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonSerializerOptions { WriteIndented = true };

        var response = new
        {
            status = healthReport.Status.ToString(),
            totalDuration = healthReport.TotalDuration.ToString(),
            entries = healthReport.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    duration = e.Value.Duration.ToString(),
                    description = e.Value.Description,
                    error = e.Value.Exception?.Message,
                    tags = e.Value.Tags
                }
            )
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}
