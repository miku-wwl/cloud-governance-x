using FinOps.Domain.Auditing;
using FinOps.Domain.CloudResources;
using FinOps.Domain.Costs;
using FinOps.Domain.Etl;
using FinOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace FinOps.Infrastructure.Persistence;

public sealed class FinOpsDbContext(DbContextOptions<FinOpsDbContext> options) : DbContext(options)
{
    public DbSet<CloudResource> CloudResources => Set<CloudResource>();

    public DbSet<CloudCostDaily> CloudCosts => Set<CloudCostDaily>();

    public DbSet<EtlJobRun> EtlJobRuns => Set<EtlJobRun>();

    public DbSet<Organization> Organizations => Set<Organization>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<ProviderConnection> ProviderConnections => Set<ProviderConnection>();

    public DbSet<CloudAccount> CloudAccounts => Set<CloudAccount>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<AuthorizationAuditEvent> AuthorizationAuditEvents =>
        Set<AuthorizationAuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FinOpsDbContext).Assembly);
    }
}
