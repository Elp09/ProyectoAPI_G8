using Microsoft.EntityFrameworkCore;
using proyecto.Models;

namespace proyecto.Data;

public partial class DataDbContext : DbContext
{
    public DataDbContext(DbContextOptions<DataDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Secret> Secrets { get; set; }

    public virtual DbSet<Source> Sources { get; set; }

    public virtual DbSet<SourceItem> SourceItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Secret>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Secrets__3214EC077E325D08");

            entity.HasIndex(e => e.SourceId, "IX_Secrets_SourceId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.KeyName).HasMaxLength(200);

            entity.HasOne(d => d.Source).WithMany(p => p.Secrets)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Secrets_Sources");
        });

        modelBuilder.Entity<Source>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Sources__3214EC07B742D7C4");

            entity.Property(e => e.AuthType)
                .HasMaxLength(50)
                .HasDefaultValue("none");
            entity.Property(e => e.ComponentType)
                .HasMaxLength(100)
                .HasDefaultValue("api");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Endpoint).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Url).HasMaxLength(500);
        });

        modelBuilder.Entity<SourceItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SourceIt__3214EC07E91DD0C2");

            entity.HasIndex(e => e.CreatedAt, "IX_SourceItems_CreatedAt").IsDescending();

            entity.HasIndex(e => e.SourceId, "IX_SourceItems_SourceId");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Endpoint).HasMaxLength(500);
            entity.Property(e => e.SavedBy).HasMaxLength(256);

            entity.HasOne(d => d.Source).WithMany(p => p.SourceItems)
                .HasForeignKey(d => d.SourceId)
                .HasConstraintName("FK_SourceItems_Sources");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
