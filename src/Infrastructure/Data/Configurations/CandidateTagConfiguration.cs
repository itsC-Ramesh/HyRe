using RC.HyRe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class CandidateTagConfiguration : IEntityTypeConfiguration<CandidateTag>
{
    public void Configure(EntityTypeBuilder<CandidateTag> builder)
    {
        builder.ToTable("candidate_tags");

        builder.HasKey(ct => new { ct.CandidateId, ct.TagId });

        builder.HasOne(ct => ct.Candidate)
            .WithMany(c => c.Tags)
            .HasForeignKey(ct => ct.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.Tag)
            .WithMany()
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
