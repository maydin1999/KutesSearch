// Data/AppDbContext.cs

using Microsoft.EntityFrameworkCore;
using KutesSearch.Models;

namespace KutesSearch.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<FileInformation> FileInformation { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileInformation>().HasKey(f => f.doc_id);
            // veya modelBuilder.Entity<FileInformation>().HasNoKey(); // Keyless entity type için

            base.OnModelCreating(modelBuilder);
        }
    }
}
