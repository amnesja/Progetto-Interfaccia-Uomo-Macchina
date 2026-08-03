using Ordo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Ordo.Services.Shared;

namespace Ordo.Services
{
    public class OrdoDbContext : DbContext
    {
        public OrdoDbContext()
        {
        }

        public OrdoDbContext(DbContextOptions<OrdoDbContext> options) : base(options)
        {
            DataGenerator.InitializeUsers(this);
        }

        public DbSet<User> Users { get; set; }
    }
}
