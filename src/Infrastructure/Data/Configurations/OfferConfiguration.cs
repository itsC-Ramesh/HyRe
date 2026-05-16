using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class OfferConfiguration : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> builder)
    {
        builder.ToTable("offers");

        builder.HasKey(o => o.Id);

        // One offer per application
        builder.HasIndex(o => o.ApplicationId)
            .IsUnique();

        builder.Property(o => o.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("INR");

        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(o => o.ContractType)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Letter document FK — nullable
        builder.HasOne(o => o.LetterDocument)
            .WithOne(d => d.OfferLetter)
            .HasForeignKey<Offer>(o => o.LetterDocId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Ignore(o => o.DomainEvents);
    }
}
