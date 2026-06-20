using FinOps.Domain.CloudResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinOps.Infrastructure.Persistence.Configurations;

internal sealed class CloudResourceConfiguration : IEntityTypeConfiguration<CloudResource>
{
    public void Configure(EntityTypeBuilder<CloudResource> builder)
    {
        builder.ToTable("cloud_resources");

        builder.HasKey(resource => resource.Id);

        builder.Property(resource => resource.Id).HasColumnName("id");
        builder.Property(resource => resource.TenantId).HasColumnName("tenant_id");
        builder.Property(resource => resource.Provider).HasColumnName("provider").HasMaxLength(32);
        builder.Property(resource => resource.AccountId).HasColumnName("account_id").HasMaxLength(128);
        builder.Property(resource => resource.ResourceId).HasColumnName("resource_id").HasMaxLength(2048);
        builder.Property(resource => resource.ResourceIdNormalized)
            .HasColumnName("resource_id_normalized")
            .HasMaxLength(2048);
        builder.Property(resource => resource.ResourceName).HasColumnName("resource_name").HasMaxLength(512);
        builder.Property(resource => resource.ResourceType).HasColumnName("resource_type").HasMaxLength(512);
        builder.Property(resource => resource.Region).HasColumnName("region").HasMaxLength(128);
        builder.Property(resource => resource.ResourceGroup).HasColumnName("resource_group").HasMaxLength(256);
        builder.Property(resource => resource.TagsJson).HasColumnName("tags_json").HasColumnType("jsonb");
        builder.Property(resource => resource.FirstSeenAt).HasColumnName("first_seen_at");
        builder.Property(resource => resource.LastSeenAt).HasColumnName("last_seen_at");

        builder.HasOne<FinOps.Domain.Tenancy.Tenant>()
            .WithMany()
            .HasForeignKey(resource => resource.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FinOps.Domain.Tenancy.CloudAccount>()
            .WithMany()
            .HasForeignKey(resource => new
            {
                resource.TenantId,
                resource.Provider,
                resource.AccountId
            })
            .HasPrincipalKey(account => new
            {
                account.TenantId,
                account.Provider,
                account.ExternalAccountId
            })
            .HasConstraintName("fk_cloud_resources_cloud_account_scope")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(resource => new
        {
            resource.TenantId,
            resource.Provider,
            resource.ResourceIdNormalized
        })
            .IsUnique()
            .HasDatabaseName("ux_cloud_resources_tenant_provider_resource_id");

        builder.HasIndex(resource => new
        {
            resource.Provider,
            resource.ResourceIdNormalized
        })
            .IsUnique()
            .HasFilter("tenant_id IS NULL")
            .HasDatabaseName("ux_cloud_resources_legacy_provider_resource_id");
    }
}
