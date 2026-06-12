using FinOps.Domain.Etl;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinOps.Infrastructure.Persistence.Configurations;

internal sealed class EtlJobRunConfiguration : IEntityTypeConfiguration<EtlJobRun>
{
    public void Configure(EntityTypeBuilder<EtlJobRun> builder)
    {
        builder.ToTable("etl_job_runs");

        builder.HasKey(run => run.Id);

        builder.Property(run => run.Id).HasColumnName("id");
        builder.Property(run => run.JobName).HasColumnName("job_name").HasMaxLength(128);
        builder.Property(run => run.Provider).HasColumnName("provider").HasMaxLength(32);
        builder.Property(run => run.StartedAt).HasColumnName("started_at");
        builder.Property(run => run.FinishedAt).HasColumnName("finished_at");
        builder.Property(run => run.Status).HasColumnName("status").HasMaxLength(32);
        builder.Property(run => run.RecordsProcessed).HasColumnName("records_processed");
        builder.Property(run => run.ErrorMessage).HasColumnName("error_message").HasMaxLength(4000);

        builder.HasIndex(run => new
        {
            run.JobName,
            run.StartedAt
        })
            .IsDescending(false, true)
            .HasDatabaseName("ix_etl_job_runs_job_name_started_at");
    }
}
