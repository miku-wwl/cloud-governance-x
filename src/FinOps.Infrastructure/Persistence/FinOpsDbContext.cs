using FinOps.Domain.CloudResources;
using FinOps.Domain.Etl;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Infrastructure.Persistence;

public sealed class FinOpsDbContext(DbContextOptions<FinOpsDbContext> options) : DbContext(options)
{
    public DbSet<CloudResource> CloudResources => Set<CloudResource>();

    public DbSet<EtlJobRun> EtlJobRuns => Set<EtlJobRun>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinOpsDbContext).Assembly);
    }
}
