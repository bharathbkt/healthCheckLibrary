using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Data;

namespace HealthCheckPOC.EntityFrameworkCore;

[ConnectionStringName("Default")]
public class HealthCheckPOCDbContext : AbpDbContext<HealthCheckPOCDbContext>
{
    // Define DbSets here

    public HealthCheckPOCDbContext(DbContextOptions<HealthCheckPOCDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        // Configuration for entities goes here
    }
}
