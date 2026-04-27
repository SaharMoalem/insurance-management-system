using Insurance.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Insurance.Api.Data.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PolicyNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.PremiumAmount)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(x => x.PolicyNumber)
            .IsUnique();

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Policies)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
