using System.ComponentModel.DataAnnotations;

namespace MbabaneHighlandersBackend2.Model
{
    public class OrderItem
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = default!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = default!;

        // Snapshot fields
        [Required, MaxLength(150)]
        public string ProductName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Color { get; set; } = string.Empty;

        [Required, MaxLength(10)]
        public string Size { get; set; } = "M";

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}
