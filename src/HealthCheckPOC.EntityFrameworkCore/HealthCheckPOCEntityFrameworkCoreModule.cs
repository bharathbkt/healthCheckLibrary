using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Oracle;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using HealthCheckPOC.Domain;

namespace HealthCheckPOC.EntityFrameworkCore;

[DependsOn(
    typeof(AbpEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreOracleModule),
    typeof(HealthCheckPOCDomainModule)
)]
public class HealthCheckPOCEntityFrameworkCoreModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAbpDbContext<HealthCheckPOCDbContext>(options =>
        {
            // Configure default repos here
            options.AddDefaultRepositories(includeAllEntities: true);
        });

        Configure<AbpDbContextOptions>(options =>
        {
            // We use standard EF Core Relational / Oracle configuration via standard setup in host
            options.UseOracle(); 
        });
    }
}
