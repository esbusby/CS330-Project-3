using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Fall2025_Project3_esbusby.Models;

namespace Fall2025_Project3_esbusby.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Fall2025_Project3_esbusby.Models.Movie> Movie { get; set; } = default!;
        public DbSet<Fall2025_Project3_esbusby.Models.Actor> Actor { get; set; } = default!;
        public DbSet<Fall2025_Project3_esbusby.Models.ActorMovie> ActorMovie { get; set; } = default!;
    }
}
