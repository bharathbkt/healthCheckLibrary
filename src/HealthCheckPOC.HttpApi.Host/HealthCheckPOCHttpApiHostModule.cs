using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using HealthCheckPOC.Application;
using HealthCheckPOC.EntityFrameworkCore;
using HealthMonitoringModule;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace HealthCheckPOC.HttpApi.Host;

[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpAutofacModule),
    typeof(HealthCheckPOCApplicationModule),
    typeof(HealthCheckPOCEntityFrameworkCoreModule),
    typeof(HealthMonitoringAbpModule) // Registers our custom health checks
)]
public class HealthCheckPOCHttpApiHostModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var timeoutSeconds = configuration.GetValue<int>("HealthChecks:TimeoutSeconds", 3);
        var timeout = TimeSpan.FromSeconds(timeoutSeconds);

        context.Services.AddControllers();
        context.Services.AddEndpointsApiExplorer();
        context.Services.AddSwaggerGen();

        // Wire up deep Oracle health check specifically to this DbContext
        context.Services.AddHealthChecks()
            .AddCheck<OracleSchemaValidationHealthCheck<HealthCheckPOCDbContext>>(
                name: "Oracle-Schema-Deep-Check",
                failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
                tags: new[] { "deep", "oracle", "database" },
                timeout: timeout);
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();

        // Register custom health check endpoints
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            // 1. Liveness Probe (immediate response, no dependency checks)
            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = CustomHealthCheckFormatter.WriteResponse
            });

            // 2. Readiness Probe (checks Kafka, Redis, MongoDB, Filepath Access)
            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("infrastructure") || 
                                     check.Tags.Contains("database") || 
                                     check.Tags.Contains("filepath"),
                ResponseWriter = CustomHealthCheckFormatter.WriteResponse
            });

            // 3. Startup/Deep Probe (Oracle Schema validation)
            endpoints.MapHealthChecks("/health/startup", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains("deep"),
                ResponseWriter = CustomHealthCheckFormatter.WriteResponse
            });
        });
    }
}
