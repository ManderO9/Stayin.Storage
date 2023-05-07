using Microsoft.EntityFrameworkCore;

namespace Stayin.Storage;

public class ApplicationDbContext : DbContext
{
    public DbSet<FileDetails> Files { get; set; }
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
}
