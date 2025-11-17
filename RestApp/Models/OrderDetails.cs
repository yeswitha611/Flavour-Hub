using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace restapp.Models
{
    public class OrderDetails
    {
        // Composite Primary Key (typically, OrderId and ItemId are used, or a single PK)
        // Let's use a single PK for simplicity
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderDetailsId { get; set; }

        // Foreign Key to the Order table
        public int OrderId { get; set; }

        // Foreign Key to the FoodItems table
        public int ItemId { get; set; }

        public decimal Price { get; set; }
        public int QuantityOrdered { get; set; }
        public decimal Amount { get; set; } // QuantityOrdered * Price

        // Navigation properties
        public Order Order { get; set; }
        public FoodItem Item { get; set; }
    }
}
