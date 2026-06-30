using FinOps.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinOps.Infrastructure.Persistence.Configurations;

internal sealed class AuthorizationAuditEventConfiguration :
    IEntityTypeConfiguration<AuthorizationAuditEvent>
{
    public void Configure(EntityTypeBuilder<AuthorizationAuditEvent> builder)
    {
        builder.ToTable("authorization_audit_events");

        builder.HasKey(auditEvent => auditEvent.Id);

        builder.Property(auditEvent => auditEvent.Id).HasColumnName("id");
        builder.Property(auditEvent => auditEvent.OccurredAt).HasColumnName("occurred_at");
        builder.Property(auditEvent => auditEvent.Permission)
            .HasColumnName("permission")
            .HasMaxLength(64);
        builder.Property(auditEvent => auditEvent.ScopeKind)
            .HasColumnName("scope_kind")
            .HasMaxLength(32);
        builder.Property(auditEvent => auditEvent.TenantId).HasColumnName("tenant_id");
        builder.Property(auditEvent => auditEvent.CloudAccountId)
            .HasColumnName("cloud_account_id");
        builder.Property(auditEvent => auditEvent.IsAllowed).HasColumnName("is_allowed");
        builder.Property(auditEvent => auditEvent.IsHighPrivilege)
            .HasColumnName("is_high_privilege");
        builder.Property(auditEvent => auditEvent.Reason)
            .HasColumnName("reason")
            .HasMaxLength(512);
        builder.Property(auditEvent => auditEvent.ActorIssuer)
            .HasColumnName("actor_issuer")
            .HasMaxLength(512);
        builder.Property(auditEvent => auditEvent.ActorSubject)
            .HasColumnName("actor_subject")
            .HasMaxLength(256);
        builder.Property(auditEvent => auditEvent.HttpMethod)
            .HasColumnName("http_method")
            .HasMaxLength(16);
        builder.Property(auditEvent => auditEvent.Path)
            .HasColumnName("path")
            .HasMaxLength(1024);
        builder.Property(auditEvent => auditEvent.StatusCode).HasColumnName("status_code");
        builder.Property(auditEvent => auditEvent.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(128);

        builder.HasOne<FinOps.Domain.Tenancy.Tenant>()
            .WithMany()
            .HasForeignKey(auditEvent => auditEvent.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(auditEvent => new
        {
            auditEvent.TenantId,
            auditEvent.OccurredAt
        })
            .IsDescending(false, true)
            .HasDatabaseName("ix_authorization_audit_events_tenant_occurred_at");

        builder.HasIndex(auditEvent => new
        {
            auditEvent.IsHighPrivilege,
            auditEvent.OccurredAt
        })
            .IsDescending(false, true)
            .HasDatabaseName("ix_authorization_audit_events_high_privilege_occurred_at");
    }
}
