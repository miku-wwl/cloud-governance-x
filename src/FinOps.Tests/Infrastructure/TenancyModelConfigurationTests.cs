using FinOps.Domain.CloudResources;
using FinOps.Domain.Costs;
using FinOps.Domain.Etl;
using FinOps.Domain.Tenancy;
using FinOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FinOps.Tests.Infrastructure;

public sealed class TenancyModelConfigurationTests
{
    [Fact]
    public void Tenant_owned_unique_indexes_include_tenant_boundary()
    {
        using var context = CreateContext();

        AssertUniqueIndex<Tenant>(
            context,
            "ux_tenants_organization_slug",
            nameof(Tenant.OrganizationId),
            nameof(Tenant.Slug));
        AssertUniqueIndex<ProviderConnection>(
            context,
            "ux_provider_connections_tenant_provider_name",
            nameof(ProviderConnection.TenantId),
            nameof(ProviderConnection.Provider),
            nameof(ProviderConnection.DisplayName));
        AssertAlternateKey<CloudAccount>(
            context,
            "ak_cloud_accounts_tenant_provider_external",
            nameof(CloudAccount.TenantId),
            nameof(CloudAccount.Provider),
            nameof(CloudAccount.ExternalAccountId));
        AssertUniqueIndex<Membership>(
            context,
            "ux_memberships_tenant_issuer_subject",
            nameof(Membership.TenantId),
            nameof(Membership.Issuer),
            nameof(Membership.Subject));
        AssertUniqueIndex<CloudResource>(
            context,
            "ux_cloud_resources_tenant_provider_resource_id",
            nameof(CloudResource.TenantId),
            nameof(CloudResource.Provider),
            nameof(CloudResource.ResourceIdNormalized));
        AssertUniqueIndex<CloudCostDaily>(
            context,
            "ux_cloud_cost_daily_tenant_identity",
            nameof(CloudCostDaily.TenantId),
            nameof(CloudCostDaily.Provider),
            nameof(CloudCostDaily.AccountId),
            nameof(CloudCostDaily.UsageDate),
            nameof(CloudCostDaily.ServiceName),
            nameof(CloudCostDaily.ResourceGroup),
            nameof(CloudCostDaily.Currency));
        AssertFilteredUniqueIndex<CloudResource>(
            context,
            "ux_cloud_resources_legacy_provider_resource_id",
            "tenant_id IS NULL",
            nameof(CloudResource.Provider),
            nameof(CloudResource.ResourceIdNormalized));
        AssertFilteredUniqueIndex<CloudCostDaily>(
            context,
            "ux_cloud_cost_daily_legacy_identity",
            "tenant_id IS NULL",
            nameof(CloudCostDaily.Provider),
            nameof(CloudCostDaily.AccountId),
            nameof(CloudCostDaily.UsageDate),
            nameof(CloudCostDaily.ServiceName),
            nameof(CloudCostDaily.ResourceGroup),
            nameof(CloudCostDaily.Currency));
    }

    [Fact]
    public void Cloud_account_connection_foreign_key_enforces_tenant_and_provider()
    {
        using var context = CreateContext();
        var accountType = context.Model.FindEntityType(typeof(CloudAccount));
        var foreignKey = Assert.Single(
            accountType!.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(ProviderConnection));

        Assert.Equal(
            [
                nameof(CloudAccount.TenantId),
                nameof(CloudAccount.ProviderConnectionId),
                nameof(CloudAccount.Provider)
            ],
            foreignKey.Properties.Select(property => property.Name));
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Theory]
    [InlineData(typeof(CloudResource))]
    [InlineData(typeof(CloudCostDaily))]
    public void Core_data_account_foreign_keys_enforce_tenant_provider_and_account(
        Type entityClrType)
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(entityClrType);
        var foreignKey = Assert.Single(
            entityType!.GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(CloudAccount));

        Assert.Equal(
            ["TenantId", "Provider", "AccountId"],
            foreignKey.Properties.Select(property => property.Name));
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void All_tenancy_relationships_restrict_cascade_delete()
    {
        using var context = CreateContext();
        Type[] tenancyTypes =
        [
            typeof(Tenant),
            typeof(ProviderConnection),
            typeof(CloudAccount),
            typeof(Membership),
            typeof(CloudResource),
            typeof(CloudCostDaily),
            typeof(EtlJobRun)
        ];

        var foreignKeys = tenancyTypes
            .Select(context.Model.FindEntityType)
            .OfType<IEntityType>()
            .SelectMany(entity => entity.GetForeignKeys())
            .ToArray();

        Assert.NotEmpty(foreignKeys);
        Assert.All(
            foreignKeys,
            foreignKey => Assert.Equal(
                DeleteBehavior.Restrict,
                foreignKey.DeleteBehavior));
    }

    private static FinOpsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FinOpsDbContext>()
            .UseNpgsql("Host=localhost;Database=finops_model;Username=finops;Password=placeholder")
            .Options;
        return new FinOpsDbContext(options);
    }

    private static void AssertUniqueIndex<TEntity>(
        FinOpsDbContext context,
        string databaseName,
        params string[] propertyNames)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        var index = Assert.Single(
            entityType!.GetIndexes(),
            candidate => candidate.GetDatabaseName() == databaseName);

        Assert.True(index.IsUnique);
        Assert.Equal(
            propertyNames,
            index.Properties.Select(property => property.Name));
    }

    private static void AssertFilteredUniqueIndex<TEntity>(
        FinOpsDbContext context,
        string databaseName,
        string filter,
        params string[] propertyNames)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        var index = Assert.Single(
            entityType!.GetIndexes(),
            candidate => candidate.GetDatabaseName() == databaseName);

        Assert.True(index.IsUnique);
        Assert.Equal(filter, index.GetFilter());
        Assert.Equal(
            propertyNames,
            index.Properties.Select(property => property.Name));
    }

    private static void AssertAlternateKey<TEntity>(
        FinOpsDbContext context,
        string databaseName,
        params string[] propertyNames)
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        var key = Assert.Single(
            entityType!.GetKeys(),
            candidate => candidate.GetName() == databaseName);

        Assert.Equal(
            propertyNames,
            key.Properties.Select(property => property.Name));
    }
}
