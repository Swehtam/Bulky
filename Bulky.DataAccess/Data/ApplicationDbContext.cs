using Bulky.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        {
            
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasData(
                new Category{ Id = 1, Name = "Action", DisplayOrder = 1 },
                new Category{ Id = 2, Name = "SciFi", DisplayOrder = 2 },
                new Category { Id = 3, Name = "History", DisplayOrder = 3 }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { 
                    Id = 1, 
                    Title = "Peter Pan", 
                    Description = "Boy who didn't want to be an adult", 
                    ISBN = "123", 
                    Author = "J. M. Barrie", 
                    ListPrice = 50, 
                    Price = 49, 
                    Price50 = 48, 
                    Price100 = 47,
                    CategoryId = 1,
                    ImageUrl = ""
                },
                new Product { 
                    Id = 2, 
                    Title = "Moby Dick", 
                    Description = "Whale named Moby Dick who swallowed two people", 
                    ISBN = "456",
                    Author = "Herman Melville",
                    ListPrice = 25.5,
                    Price = 24.5,
                    Price50 = 23.5,
                    Price100 = 22.5,
                    CategoryId = 2,
                    ImageUrl = ""
                },
                new Product { 
                    Id = 3, 
                    Title = "Pinochio", 
                    Description = "Wood boy who wants to be a real boy", 
                    ISBN = "789",
                    Author = "Carlo Collodi",
                    ListPrice = 9.99,
                    Price = 8.99,
                    Price50 = 7.99,
                    Price100 = 6.99,
                    CategoryId = 3,
                    ImageUrl = ""
                }
            );
        }
    }
}
