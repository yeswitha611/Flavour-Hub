using Microsoft.EntityFrameworkCore;
using restapp.Models;

namespace restapp.Dal
{
    public class RestContext : DbContext
    {
        public RestContext(DbContextOptions<RestContext> options) : base(options)  
        { 
            //lazy loading can be implemented here
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //manage database operations like querying, saving, and configuring relationships.
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Role>().ToTable("Role");
            modelBuilder.Entity<User>().ToTable("User");
            modelBuilder.Entity<Sliders>().ToTable("Sliders");
            modelBuilder.Entity<Category>().ToTable("Category");
            modelBuilder.Entity<FoodItem>().ToTable("FoodItem");
            modelBuilder.Entity<ItemType>().ToTable("ItemType");
            modelBuilder.Entity<Order>().ToTable("Order");
            modelBuilder.Entity<OrderDetails>().ToTable("OrderDetails");
            modelBuilder.Entity<Payment>().ToTable("Payment");


            // Configure foreign key mappings
            //It allows you to access the user who placed the order (order.User)
            //It allows you to access the user who placed the payment(payment.User)
            modelBuilder.Entity<Order>()
                //Each Order has one related User.
                .HasOne(o => o.User)
                //The User can be associated with many orders.
                .WithMany()
                //specifing the Id is the foriegn key in order
                .HasForeignKey(o => o.Id)
                //specifing that the Id in user is pk
                .HasPrincipalKey(u => u.Id);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.Id)
                .HasPrincipalKey(u => u.Id);


            // Tell EF Core to completely ignore the CartItem class
            modelBuilder.Ignore<CartItem>();

        }
        //dbset properties
        //allows to save entities
        public DbSet<Role> roles { get; set; }
        public DbSet<User> users { get; set; }
        public DbSet<Sliders> sliders { get; set; }
        public DbSet<Category> categories { get; set; }
        public DbSet<ItemType> itemTypes { get; set; }
        public DbSet<FoodItem> fooditems { get; set; }
        public DbSet<CartItem> Carts { get; set; }
        public DbSet<Order> orders { get; set; }
        public DbSet<OrderDetails> orderdetails { get; set; }
        public DbSet<Payment> payments { get; set; }




        public DbSet<restapp.Models.UserLogin> UserLogin { get; set; } = default!;
        public DbSet<restapp.Models.FoodItem> FoodItem { get; set; } = default!;
    }
}
