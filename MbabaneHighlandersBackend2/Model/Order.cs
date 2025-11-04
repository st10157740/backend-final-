using System.ComponentModel.DataAnnotations;

namespace MbabaneHighlandersBackend2.Model
{
    public class Order
    {
        public int Id { get; set; }

        [MaxLength(20)]
        public string OrderCode { get; set; } = string.Empty;

        // Customer details
        [Required, MaxLength(200)]
        public string CustomerFullName { get; set; } = string.Empty;

        [Required, EmailAddress, MaxLength(256)]
        public string CustomerEmail { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? CustomerPhone { get; set; }

        [Required, MaxLength(300)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Status { get; set; } = "Pending"; // Pending, Paid, Cancelled

        public decimal TotalAmount { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }
}
