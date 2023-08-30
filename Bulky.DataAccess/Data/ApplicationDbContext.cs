using Bulky.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bulky.DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext<IdentityUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails{ get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>().HasData(
                new Category{ Id = 1, Name = "Action", DisplayOrder = 1 },
                new Category{ Id = 2, Name = "SciFi", DisplayOrder = 2 },
                new Category { Id = 3, Name = "History", DisplayOrder = 3 }
            );

			modelBuilder.Entity<Company>().HasData(
				new Company
				{
					Id = 1,
					Name = "Tech Solution",
					StreetAddress = "123 Tech St",
					City = "Tech City",
					PostalCode = "12121",
					State = "IL",
					PhoneNumber = "6669990000"
				},
				new Company
				{
					Id = 2,
					Name = "Vivid Books",
					StreetAddress = "999 Vid St",
					City = "Vid City",
					PostalCode = "66666",
					State = "IL",
					PhoneNumber = "7779990000"
				},
				new Company
				{
					Id = 3,
					Name = "Readers Club",
					StreetAddress = "999 Main St",
					City = "Lala land",
					PostalCode = "99999",
					State = "NY",
					PhoneNumber = "1113335555"
				}
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
