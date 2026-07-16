using Microsoft.EntityFrameworkCore;
using XcavateProfile.Client;

namespace XcavateProfileApi.Data;

[Serializable]
public class ProfileDbContext : DbContext
{
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options)
        : base(options)
    {
    }

    public DbSet<Profile> Profiles { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure Profile entity
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(p => p.Ss58Address);
            entity.Property(p => p.Ss58Address).IsRequired();

            entity.Property(p => p.Nickname).IsRequired(false);
            entity.HasIndex(p => p.Nickname).IsUnique();

            entity.Property(p => p.Bio).IsRequired(false);
            entity.Property(p => p.ProfilePicture).IsRequired(false);
            entity.Property(p => p.X25519Key).IsRequired(false);
        });
    }
}
