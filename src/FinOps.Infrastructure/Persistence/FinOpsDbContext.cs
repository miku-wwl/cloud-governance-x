using FinOps.Domain.CloudResources;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Infrastructure.Persistence;

public sealed class FinOpsDbContext(DbContextOptions<FinOpsDbContext> options) : DbContext(options)
{
    public DbSet<CloudResource> CloudResources => Set<CloudResource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinOpsDbContext).Assembly);
    }
}
