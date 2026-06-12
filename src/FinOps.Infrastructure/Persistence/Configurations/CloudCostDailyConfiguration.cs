using FinOps.Domain.Costs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinOps.Infrastructure.Persistence.Configurations;

internal sealed class CloudCostDailyConfiguration : IEntityTypeConfiguration<CloudCostDaily>
{
    public void Configure(EntityTypeBuilder<CloudCostDaily> builder)
    {
        builder.ToTable("cloud_cost_daily");

        builder.HasKey(cost => cost.Id);

        builder.Property(cost => cost.Id).HasColumnName("id");
        builder.Property(cost => cost.Provider).HasColumnName("provider").HasMaxLength(32);
        builder.Property(cost => cost.AccountId).HasColumnName("account_id").HasMaxLength(128);
        builder.Property(cost => cost.UsageDate).HasColumnName("usage_date");
        builder.Property(cost => cost.ServiceName).HasColumnName("service_name").HasMaxLength(256);
        builder.Property(cost => cost.ResourceGroup).HasColumnName("resource_group").HasMaxLength(256);
        builder.Property(cost => cost.Cost).HasColumnName("cost").HasPrecision(20, 8);
        builder.Property(cost => cost.Currency).HasColumnName("currency").HasMaxLength(16);
        builder.Property(cost => cost.RawJson).HasColumnName("raw_json").HasColumnType("jsonb");

        builder.HasIndex(cost => new
        {
            cost.Provider,
            cost.AccountId,
            cost.UsageDate,
            cost.ServiceName,
            cost.ResourceGroup,
            cost.Currency
        })
            .IsUnique()
            .HasDatabaseName("ux_cloud_cost_daily_identity");
    }
}
