using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace restapp.Models
{
    public class Order
    {

        // Primary Key (Identity Column in DB)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderId { get; set; }

            // Foreign Key to the User table (Id)
            public int Id { get; set; }

            // Order date/time
            public DateTime OrderDate { get; set; } = DateTime.Now;

            // Navigation properties (optional, but good practice for Entity Framework)
            public User User { get; set; }
            public ICollection<OrderDetails> OrderDetails { get; set; }
        }
    }

