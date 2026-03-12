using Volo.Abp.Application;
using Volo.Abp.Modularity;
using HealthCheckPOC.Domain.Shared;

namespace HealthCheckPOC.Application.Contracts;

[DependsOn(
    typeof(AbpDddApplicationContractsModule),
    typeof(HealthCheckPOCDomainSharedModule)
)]
public class HealthCheckPOCApplicationContractsModule : AbpModule
{
}
