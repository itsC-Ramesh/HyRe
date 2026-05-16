using RC.HyRe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RC.HyRe.Infrastructure.Data.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasIndex(r => r.Token).IsUnique();
        builder.HasIndex(r => r.UserId);
        
        builder.HasOne<Identity.ApplicationUser>()
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
