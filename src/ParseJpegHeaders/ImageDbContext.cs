using Microsoft.EntityFrameworkCore;
using ParseJpegHeaders.Models;

namespace ParseJpegHeaders;
public class ImageDbContext : DbContext
{
    public DbSet<Image> Images { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=127.0.0.1,1433;Database=ImageDb;User Id=sa;Password=GZLCS!^S(kx9;Trust Server Certificate=True;");
    }

}
