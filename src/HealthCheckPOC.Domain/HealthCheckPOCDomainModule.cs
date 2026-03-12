using Volo.Abp.Domain;
using Volo.Abp.Modularity;
using HealthCheckPOC.Domain.Shared;

namespace HealthCheckPOC.Domain;

[DependsOn(
    typeof(AbpDddDomainModule),
    typeof(HealthCheckPOCDomainSharedModule)
)]
public class HealthCheckPOCDomainModule : AbpModule
{
}
