using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace restapp.Models
{
    public class Payment
    {

        // Primary Key (Identity Column in DB)
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaymentId { get; set; }

        // Foreign Key to the User table
        public int Id { get; set; }

        public decimal BillAmount { get; set; }
        public string PaymentMethod { get; set; }
        public bool Status { get; set; } // true for successful/completed, false otherwise

        // Navigation property
        public User User { get; set; }
    }
}