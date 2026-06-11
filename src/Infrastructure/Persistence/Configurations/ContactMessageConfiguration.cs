using Domain.Aggregates.Contact;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("contact_messages", "identity");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.SenderName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.SenderEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(m => m.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Body)
            .IsRequired()
            .HasMaxLength(5000);

        builder.Property(m => m.ReceivedAt)
            .IsRequired();

        builder.Ignore(m => m.DomainEvents);
    }
}
