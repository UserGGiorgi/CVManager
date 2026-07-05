using CVManager.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CVManager.Data;

public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Core.Entities.Attribute> Attributes { get; set; }
    public DbSet<ProfileAttribute> ProfileAttributes { get; set; }
    public DbSet<Position> Positions { get; set; }
    public DbSet<PositionAttribute> PositionAttributes { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<CV> CVs { get; set; }
    public DbSet<CVAttributeValue> CVAttributeValues { get; set; }
    public DbSet<CVLike> CVLikes { get; set; }
    public DbSet<DiscussionPost> DiscussionPosts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // User configuration
        builder.Entity<User>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Location).HasMaxLength(200);
            entity.Property(u => u.Language).HasMaxLength(5).HasDefaultValue("en");
            entity.Property(u => u.Theme).HasMaxLength(10).HasDefaultValue("light");
        });

        // Attribute configuration
        builder.Entity<Core.Entities.Attribute>(entity =>
        {
            entity.HasIndex(a => a.Name).IsUnique();
            entity.Property(a => a.Name).HasMaxLength(200).IsRequired();
            entity.Property(a => a.Category).HasMaxLength(100).IsRequired();
            entity.HasOne(a => a.Creator)
                  .WithMany()
                  .HasForeignKey(a => a.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ProfileAttribute configuration
        builder.Entity<ProfileAttribute>(entity =>
        {
            entity.HasIndex(pa => new { pa.UserId, pa.AttributeId }).IsUnique();
            entity.Property(pa => pa.Version).IsRowVersion();
            entity.HasOne(pa => pa.User)
                  .WithMany(u => u.ProfileAttributes)
                  .HasForeignKey(pa => pa.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pa => pa.Attribute)
                  .WithMany(a => a.ProfileAttributes)
                  .HasForeignKey(pa => pa.AttributeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Position configuration
        builder.Entity<Position>(entity =>
        {
            entity.Property(p => p.Title).HasMaxLength(200).IsRequired();
            entity.HasOne(p => p.Creator)
                  .WithMany()
                  .HasForeignKey(p => p.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // PositionAttribute configuration
        builder.Entity<PositionAttribute>(entity =>
        {
            entity.HasIndex(pa => new { pa.PositionId, pa.AttributeId }).IsUnique();
            entity.HasOne(pa => pa.Position)
                  .WithMany(p => p.PositionAttributes)
                  .HasForeignKey(pa => pa.PositionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pa => pa.Attribute)
                  .WithMany(a => a.PositionAttributes)
                  .HasForeignKey(pa => pa.AttributeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Project>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(200).IsRequired();

            entity.Property(p => p.Technologies)
                  .HasConversion(
                      v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                      v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
                  )
                  .Metadata.SetValueComparer(
                      new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<string>>(
                          (c1, c2) => c1!.SequenceEqual(c2!),
                          c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                          c => c.ToList()
                      )
                  );

            entity.HasOne(p => p.User)
                  .WithMany(u => u.Projects)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // CV configuration
        builder.Entity<CV>(entity =>
        {
            entity.HasIndex(cv => new { cv.PositionId, cv.CandidateId }).IsUnique();
            entity.Property(cv => cv.Version).IsRowVersion();
            entity.HasOne(cv => cv.Position)
                  .WithMany(p => p.CVs)
                  .HasForeignKey(cv => cv.PositionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(cv => cv.Candidate)
                  .WithMany(u => u.CVs)
                  .HasForeignKey(cv => cv.CandidateId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CVAttributeValue configuration
        builder.Entity<CVAttributeValue>(entity =>
        {
            entity.HasIndex(cav => new { cav.CVId, cav.AttributeId }).IsUnique();
            entity.HasOne(cav => cav.CV)
                  .WithMany(cv => cv.AttributeValues)
                  .HasForeignKey(cav => cav.CVId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(cav => cav.Attribute)
                  .WithMany()
                  .HasForeignKey(cav => cav.AttributeId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // CVLike configuration
        builder.Entity<CVLike>(entity =>
        {
            entity.HasKey(cl => new { cl.CVId, cl.RecruiterId });
            entity.HasOne(cl => cl.CV)
                  .WithMany(cv => cv.Likes)
                  .HasForeignKey(cl => cl.CVId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(cl => cl.Recruiter)
                  .WithMany(u => u.GivenLikes)
                  .HasForeignKey(cl => cl.RecruiterId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // DiscussionPost configuration
        builder.Entity<DiscussionPost>(entity =>
        {
            entity.Property(dp => dp.Content).IsRequired();
            entity.HasOne(dp => dp.Position)
                  .WithMany(p => p.DiscussionPosts)
                  .HasForeignKey(dp => dp.PositionId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(dp => dp.Author)
                  .WithMany(u => u.DiscussionPosts)
                  .HasForeignKey(dp => dp.AuthorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}