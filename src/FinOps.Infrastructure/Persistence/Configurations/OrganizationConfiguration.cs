using FinOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinOps.Infrastructure.Persistence.Configurations;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Id).HasColumnName("id");
        builder.Property(organization => organization.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256);
        builder.Property(organization => organization.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(organization => organization.CreatedAt).HasColumnName("created_at");
        builder.Property(organization => organization.UpdatedAt).HasColumnName("updated_at");
    }
}
