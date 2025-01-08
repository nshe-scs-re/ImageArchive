using Microsoft.EntityFrameworkCore;
using ParseJpegHeaders.Models;

namespace ParseJpegHeaders;
public class ImageDbContext : DbContext
{
    public DbSet<Image> Images { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("");
    }

}
