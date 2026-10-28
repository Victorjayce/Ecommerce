using Ecommerce.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Data
{
    public class DataContext : IdentityDbContext<User>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<User> UsersTable { get; set; }
        public DbSet<RefreshToken> RefreshTokensTable { get; set; }
        public DbSet<Product> ProductsTable {get; set;}
        public DbSet<CartItem> CartItemsTable { get; set; }
        public DbSet<Order> OrdersTable { get; set; }
        public DbSet<OrderItem> OrderItemsTable { get; set; }
        public DbSet<Payment> PaymentsTable { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<CartItem>()
                .HasIndex(c => c.ProductId)
                .IsUnique();

            modelBuilder.Entity<Order>(e =>
            {
                e.Property(o => o.Status)
                .HasConversion<string>();

                e.Property(o => o.PaymentStatus)
                .HasConversion<string>();
            });
                

            modelBuilder.Entity<Payment>(e =>
            {
                e.Property(o => o.Status)
                 .HasConversion<string>();

                e.HasIndex(p => p.Reference).IsUnique();

                e.HasOne(p => p.Order)
                 .WithMany()
                 .HasForeignKey(p => p.OrderId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(p => p.User)
                 .WithMany()
                 .HasForeignKey(p => p.UserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });


            modelBuilder.Entity<CartItem>()
                .HasOne(c => c.Product)
                .WithMany()
                .HasForeignKey(c => c.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
