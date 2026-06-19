using FinOps.Application.Cloud;
using FinOps.Application.Tenancy;
using FinOps.Domain.Tenancy;
using FinOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FinOps.Tests.Infrastructure;

public sealed class TenantRepositoryIntegrationTests
{
    [PostgreSqlIntegrationFact]
    [Trait("Category", "PostgreSqlIntegration")]
    public async Task Repositories_isolate_tenant_reads_writes_and_id_updates()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            "FINOPS_TENANT_TEST_CONNECTION");
        Assert.False(string.IsNullOrWhiteSpace(connectionString));

        var options = new DbContextOptionsBuilder<FinOpsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        Guid tenantAId;
        Guid tenantBId;
        await using (var setupContext = new FinOpsDbContext(options))
        {
            var organization = Organization.Create(
                "Repository Integration",
                DateTimeOffset.UtcNow);
            var tenantAEntity = Tenant.Create(
                organization.Id,
                $"tenant-a-{Guid.NewGuid():N}"[..20],
                "Tenant A",
                DateTimeOffset.UtcNow);
            var tenantBEntity = Tenant.Create(
                organization.Id,
                $"tenant-b-{Guid.NewGuid():N}"[..20],
                "Tenant B",
                DateTimeOffset.UtcNow);
            var connectionA = ProviderConnection.Create(
                tenantAEntity.Id,
                "Azure",
                "Tenant A connection",
                $"workload://{tenantAEntity.Id}",
                DateTimeOffset.UtcNow);
            var connectionB = ProviderConnection.Create(
                tenantBEntity.Id,
                "Azure",
                "Tenant B connection",
                $"workload://{tenantBEntity.Id}",
                DateTimeOffset.UtcNow);
            var accountA = CloudAccount.Create(
                tenantAEntity.Id,
                connectionA.Id,
                "Azure",
                "tenant-a-account",
                null,
                "Shared account A",
                null,
                DateTimeOffset.UtcNow);
            var accountB = CloudAccount.Create(
                tenantBEntity.Id,
                connectionB.Id,
                "Azure",
                "tenant-b-account",
                null,
                "Shared account B",
                null,
                DateTimeOffset.UtcNow);
            setupContext.AddRange(
                organization,
                tenantAEntity,
                tenantBEntity,
                connectionA,
                connectionB,
                accountA,
                accountB);
            await setupContext.SaveChangesAsync();
            tenantAId = tenantAEntity.Id;
            tenantBId = tenantBEntity.Id;
        }

        var resourceA = new CloudResourceDto(
            "Azure",
            "tenant-a-account",
            "/subscriptions/shared/resourceGroups/rg/providers/demo/type/item",
            "shared-resource",
            "demo/type",
            "australiaeast",
            "rg",
            new Dictionary<string, string>());
        var resourceB = resourceA with { AccountId = "tenant-b-account" };
        var costA = new CloudCostDailyDto(
            "Azure",
            "tenant-a-account",
            new DateOnly(2026, 6, 19),
            "Storage",
            "rg",
            10m,
            "NZD",
            "{}");
        var costB = costA with { AccountId = "tenant-b-account" };

        var tenantA = CreateTenantContext(tenantAId);
        var tenantB = CreateTenantContext(tenantBId);

        await using (var tenantAContext = new FinOpsDbContext(options))
        {
            var resourceRepository = new CloudResourceRepository(
                tenantAContext,
                tenantA);
            var costRepository = new CloudCostRepository(tenantAContext, tenantA);

            await resourceRepository.UpsertAsync([resourceA], DateTimeOffset.UtcNow);
            await costRepository.UpsertAsync([costA]);
        }

        Guid tenantAJobId;
        var factory = new PooledDbContextFactory<FinOpsDbContext>(options);
        var tenantAJobs = new EtlJobRunRepository(factory, tenantA);
        tenantAJobId = await tenantAJobs.StartAsync(
            "tenant-isolation",
            "Azure",
            DateTimeOffset.UtcNow);

        await using (var tenantBContext = new FinOpsDbContext(options))
        {
            var resourceRepository = new CloudResourceRepository(
                tenantBContext,
                tenantB);
            var costRepository = new CloudCostRepository(tenantBContext, tenantB);

            var resourceResult = await resourceRepository.UpsertAsync(
                [resourceB],
                DateTimeOffset.UtcNow);
            var costResult = await costRepository.UpsertAsync([costB]);

            Assert.Equal(1, resourceResult.Inserted);
            Assert.Equal(1, costResult.Inserted);
        }

        var tenantBCostQueries = new CloudCostQueryRepository(factory, tenantB);
        var tenantBCosts = await tenantBCostQueries.GetDailyAsync(
            "Azure",
            new DateOnly(2026, 6, 19),
            new DateOnly(2026, 6, 19));
        Assert.Single(tenantBCosts);
        Assert.Equal(10m, tenantBCosts[0].Cost);

        await using (var invalidScopeContext = new FinOpsDbContext(options))
        {
            var repository = new CloudResourceRepository(
                invalidScopeContext,
                tenantA);
            await Assert.ThrowsAsync<DbUpdateException>(() =>
                repository.UpsertAsync([resourceB], DateTimeOffset.UtcNow));
        }

        var tenantBJobs = new EtlJobRunRepository(factory, tenantB);
        Assert.Empty(await tenantBJobs.GetRecentAsync("tenant-isolation", 20));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            tenantBJobs.CompleteAsync(
                tenantAJobId,
                DateTimeOffset.UtcNow,
                1));

        await tenantAJobs.CompleteAsync(
            tenantAJobId,
            DateTimeOffset.UtcNow,
            1);

        await using var verificationContext = new FinOpsDbContext(options);
        Assert.Equal(
            2,
            await verificationContext.CloudResources.CountAsync(resourceRow =>
                resourceRow.ResourceIdNormalized ==
                "/SUBSCRIPTIONS/SHARED/RESOURCEGROUPS/RG/PROVIDERS/DEMO/TYPE/ITEM"));
        Assert.Equal(
            2,
            await verificationContext.CloudCosts.CountAsync(costRow =>
                costRow.UsageDate == new DateOnly(2026, 6, 19)));
    }

    [Fact]
    public async Task Every_repository_fails_without_tenant_context()
    {
        var options = new DbContextOptionsBuilder<FinOpsDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options;
        var missing = new TenantContext();
        await using var context = new FinOpsDbContext(options);
        var factory = new PooledDbContextFactory<FinOpsDbContext>(options);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CloudResourceRepository(context, missing)
                .UpsertAsync([], DateTimeOffset.UtcNow));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CloudCostRepository(context, missing).UpsertAsync([]));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new CloudCostQueryRepository(factory, missing).GetDailyAsync(
                "Azure",
                DateOnly.MinValue,
                DateOnly.MaxValue));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new EtlJobRunRepository(factory, missing).GetRecentAsync(null, 20));
    }

    private static TenantContext CreateTenantContext(Guid tenantId)
    {
        var context = new TenantContext();
        ((ITenantContextInitializer)context).Initialize(
            TrustedTenantContext.ForBackgroundJob(tenantId));
        return context;
    }
}

public sealed class PostgreSqlIntegrationFactAttribute : FactAttribute
{
    public PostgreSqlIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(
            "FINOPS_TENANT_TEST_CONNECTION")))
        {
            Skip =
                "Requires FINOPS_TENANT_TEST_CONNECTION and a migrated PostgreSQL database.";
        }
    }
}
