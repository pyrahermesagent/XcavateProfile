using Microsoft.EntityFrameworkCore;
using XcavateProfile.Client;

namespace XcavateProfileApi.Data;

public static class ModelBuilderExtensions
{
    public static void ConfigureProfile(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(p => p.Ss58Address);
            entity.Property(p => p.Ss58Address).IsRequired().HasColumnType("varchar(64)");

            entity.Property(p => p.Nickname).HasColumnType("varchar(64)");
            entity.HasIndex(p => p.Nickname).IsUnique();

            entity.Property(p => p.Bio).HasColumnType("text");
            entity.Property(p => p.ProfilePicture).HasColumnType("text");
            entity.Property(p => p.X25519Key).HasColumnType("varchar(64)");
        });
    }
}
