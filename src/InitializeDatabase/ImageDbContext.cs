using Microsoft.EntityFrameworkCore;

namespace InitializeDatabase;

public class ImageDbContext : DbContext
{
    public ImageDbContext() {}
    public ImageDbContext(DbContextOptions<ImageDbContext> options) : base(options){}

    public DbSet<Image> Images { get; set; }
    public DbSet<UserQuery> UserQueries { get; set; }
}
