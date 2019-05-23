using Microsoft.EntityFrameworkCore;

namespace Library.API.Entities
{
    public sealed class LibraryContext : DbContext
    {
        public LibraryContext(DbContextOptions options)
            : base(options)
        {
            Database.Migrate();
        }

        public DbSet<Author> Authors { get; set; }
        public DbSet<Book> Books { get; set; }
    }
}