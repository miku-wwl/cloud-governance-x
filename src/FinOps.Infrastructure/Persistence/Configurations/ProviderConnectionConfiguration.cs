using FinOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinOps.Infrastructure.Persistence.Configurations;

internal sealed class ProviderConnectionConfiguration :
    IEntityTypeConfiguration<ProviderConnection>
{
    public void Configure(EntityTypeBuilder<ProviderConnection> builder)
    {
        builder.ToTable("provider_connections");
        builder.HasKey(connection => connection.Id);

        builder.Property(connection => connection.Id).HasColumnName("id");
        builder.Property(connection => connection.TenantId).HasColumnName("tenant_id");
        builder.Property(connection => connection.Provider)
            .HasColumnName("provider")
            .HasMaxLength(32);
        builder.Property(connection => connection.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256);
        builder.Property(connection => connection.CredentialReference)
            .HasColumnName("credential_reference")
            .HasMaxLength(1024);
        builder.Property(connection => connection.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(connection => connection.LastValidatedAt)
            .HasColumnName("last_validated_at");
        builder.Property(connection => connection.CreatedAt).HasColumnName("created_at");
        builder.Property(connection => connection.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(connection => connection.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasAlternateKey(connection => new
        {
            connection.TenantId,
            connection.Id,
            connection.Provider
        });

        builder.HasIndex(connection => new
        {
            connection.TenantId,
            connection.Provider,
            connection.DisplayName
        })
            .IsUnique()
            .HasDatabaseName("ux_provider_connections_tenant_provider_name");

        builder.HasIndex(connection => new
        {
            connection.TenantId,
            connection.Provider,
            connection.CredentialReference
        })
            .IsUnique()
            .HasDatabaseName("ux_provider_connections_tenant_provider_credential");
    }
}
