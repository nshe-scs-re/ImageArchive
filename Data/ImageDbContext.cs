using ExtractImagePaths.Models;
using Microsoft.EntityFrameworkCore;

namespace ExtractImagePaths.Data
{
    public class ImageDbContext(DbContextOptions options) : DbContext(options)
    {
        public DbSet<Image> Images { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Image>()
                .Property(i => i.Id)
                .ValueGeneratedOnAdd();
        }
    }
}
