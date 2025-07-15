using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<City> Cities { get; set; }
        public DbSet<ZipCode> ZipCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<City>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                      .IsRequired()
                      .HasColumnType("nchar(32)");
            });

            modelBuilder.Entity<ZipCode>(entity =>
            {
                entity.HasKey(e => new { e.CityId, e.Zip });
                entity.Property(e => e.Zip)
                      .IsRequired()
                      .HasColumnType("nchar(5)");
                entity.HasOne(e => e.City)
                      .WithMany(c => c.ZipCodes)
                      .HasForeignKey(e => e.CityId);
            });
        }
    }

    public class City
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nchar(32)")]
        public string Name { get; set; }

        public ICollection<ZipCode> ZipCodes { get; set; }
    }

    public class ZipCode
    {
        [Key, Column(Order = 0)]
        public int CityId { get; set; }

        [Key, Column(Order = 1)]
        [Required]
        public string Zip { get; set; }

        [ForeignKey("CityId")]
        public City City { get; set; }
    }
}
