using ContactLandingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace ContactLandingApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Contact>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Correo).IsUnique();
            entity.HasIndex(x => x.Codigo).IsUnique();

            entity.Property(x => x.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(x => x.Apellidos).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Telefono).IsRequired().HasMaxLength(30);
            entity.Property(x => x.Correo).IsRequired().HasMaxLength(150);
            entity.Property(x => x.Empresa).HasMaxLength(150);
            entity.Property(x => x.Codigo).IsRequired().HasMaxLength(50);
            entity.Property(x => x.UsoCodigo).HasDefaultValue(false);
            entity.Property(x => x.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
