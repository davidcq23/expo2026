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
            entity.ToTable("contacts");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Nombre).HasColumnName("nombre").IsRequired().HasMaxLength(100);
            entity.Property(x => x.Apellidos).HasColumnName("apellidos").IsRequired().HasMaxLength(150);
            entity.Property(x => x.Telefono).HasColumnName("telefono").IsRequired().HasMaxLength(30);
            entity.Property(x => x.Correo).HasColumnName("correo").IsRequired().HasMaxLength(150);
            entity.Property(x => x.Empresa).HasColumnName("empresa").HasMaxLength(150);
            entity.Property(x => x.Codigo).HasColumnName("codigo").IsRequired().HasMaxLength(50);
            entity.Property(x => x.UsoCodigo).HasColumnName("uso_codigo").HasDefaultValue(false);
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(x => x.Correo).IsUnique();
            entity.HasIndex(x => x.Codigo).IsUnique();
        });
    }
}
