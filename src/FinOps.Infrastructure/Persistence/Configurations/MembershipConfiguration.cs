using FinOps.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinOps.Infrastructure.Persistence.Configurations;

internal sealed class MembershipConfiguration : IEntityTypeConfiguration<Membership>
{
    public void Configure(EntityTypeBuilder<Membership> builder)
    {
        builder.ToTable("memberships");
        builder.HasKey(membership => membership.Id);

        builder.Property(membership => membership.Id).HasColumnName("id");
        builder.Property(membership => membership.TenantId).HasColumnName("tenant_id");
        builder.Property(membership => membership.Issuer)
            .HasColumnName("issuer")
            .HasMaxLength(512);
        builder.Property(membership => membership.Subject)
            .HasColumnName("subject")
            .HasMaxLength(256);
        builder.Property(membership => membership.SubjectType)
            .HasColumnName("subject_type")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(membership => membership.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(256);
        builder.Property(membership => membership.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(membership => membership.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(membership => membership.CreatedAt).HasColumnName("created_at");
        builder.Property(membership => membership.UpdatedAt).HasColumnName("updated_at");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(membership => new
        {
            membership.TenantId,
            membership.Issuer,
            membership.Subject
        })
            .IsUnique()
            .HasDatabaseName("ux_memberships_tenant_issuer_subject");
    }
}
