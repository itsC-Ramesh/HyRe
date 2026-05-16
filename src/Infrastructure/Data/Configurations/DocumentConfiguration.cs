using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RC.HyRe.Domain.Entities;
using RC.HyRe.Domain.Enums;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.FileKey)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(d => d.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(d => d.MimeType)
            .IsRequired()
            .HasMaxLength(255);

        // Polymorphic lookup index: find all docs for a given entity
        builder.HasIndex(d => new { d.EntityType, d.EntityId });

        // Back-navigation props are configured on the owning side (Candidate, Offer)
        // so EF doesn't try to set up reverse FKs from Document → them.
        builder.Ignore(d => d.CandidateResume);
        builder.Ignore(d => d.OfferLetter);
        builder.Ignore(d => d.DomainEvents);
    }
}
