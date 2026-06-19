using FinOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinOps.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(tenant => tenant.Id);

        builder.Property(tenant => tenant.Id).HasColumnName("id");
        builder.Property(tenant => tenant.OrganizationId).HasColumnName("organization_id");
        builder.Property(tenant => tenant.Slug).HasColumnName("slug").HasMaxLength(63);
        builder.Property(tenant => tenant.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256);
        builder.Property(tenant => tenant.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(tenant => tenant.CreatedAt).HasColumnName("created_at");
        builder.Property(tenant => tenant.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(tenant => tenant.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(tenant => new
        {
            tenant.OrganizationId,
            tenant.Slug
        })
            .IsUnique()
            .HasDatabaseName("ux_tenants_organization_slug");
    }
}
