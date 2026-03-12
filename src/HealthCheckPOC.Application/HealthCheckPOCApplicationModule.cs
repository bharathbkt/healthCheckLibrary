using Volo.Abp.Application;
using Volo.Abp.Modularity;
using HealthCheckPOC.Domain;
using HealthCheckPOC.Application.Contracts;

namespace HealthCheckPOC.Application;

[DependsOn(
    typeof(AbpDddApplicationModule),
    typeof(HealthCheckPOCDomainModule),
    typeof(HealthCheckPOCApplicationContractsModule)
)]
public class HealthCheckPOCApplicationModule : AbpModule
{
}
