using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SubscriberService.Domain.Entities;

namespace SubscriberService.Infrastructure.Persistence.Configurations;

public class SubscriberEntityTypeConfig : IEntityTypeConfiguration<Subscriber>
{
    public void Configure(EntityTypeBuilder<Subscriber> b)
    {
        b.ToTable("Subscribers");
        b.HasKey(x => x.Id);

        b.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        // Ensure uniqueness (idempotent subscribe)
        b.HasIndex(x => x.Email).IsUnique();

        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
    }
}