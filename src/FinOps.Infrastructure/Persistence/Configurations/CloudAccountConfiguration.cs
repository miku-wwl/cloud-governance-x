using FinOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinOps.Infrastructure.Persistence.Configurations;

internal sealed class CloudAccountConfiguration : IEntityTypeConfiguration<CloudAccount>
{
    public void Configure(EntityTypeBuilder<CloudAccount> builder)
    {
        builder.ToTable("cloud_accounts");
        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id).HasColumnName("id");
        builder.Property(account => account.TenantId).HasColumnName("tenant_id");
        builder.Property(account => account.ProviderConnectionId)
            .HasColumnName("provider_connection_id");
        builder.Property(account => account.Provider)
            .HasColumnName("provider")
            .HasMaxLength(32);
        builder.Property(account => account.ExternalAccountId)
            .HasColumnName("external_account_id")
            .HasMaxLength(256);
        builder.Property(account => account.ProviderDirectoryId)
            .HasColumnName("provider_directory_id")
            .HasMaxLength(256);
        builder.Property(account => account.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256);
        builder.Property(account => account.Environment)
            .HasColumnName("environment")
            .HasMaxLength(64);
        builder.Property(account => account.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(account => account.CreatedAt).HasColumnName("created_at");
        builder.Property(account => account.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(account => account.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProviderConnection>()
            .WithMany()
            .HasForeignKey(account => new
            {
                account.TenantId,
                account.ProviderConnectionId,
                account.Provider
            })
            .HasPrincipalKey(connection => new
            {
                connection.TenantId,
                connection.Id,
                connection.Provider
            })
            .HasConstraintName("fk_cloud_accounts_provider_connection_scope")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(account => new
        {
            account.TenantId,
            account.Provider,
            account.ExternalAccountId
        })
            .IsUnique()
            .HasDatabaseName("ux_cloud_accounts_tenant_provider_external");

        builder.HasIndex(account => new
        {
            account.Provider,
            account.ExternalAccountId
        })
            .IsUnique()
            .HasDatabaseName("ux_cloud_accounts_provider_external");
    }
}
