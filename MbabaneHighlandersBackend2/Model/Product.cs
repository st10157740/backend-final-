using System.ComponentModel.DataAnnotations;

namespace MbabaneHighlandersBackend2.Model
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Color { get; set; } = string.Empty;

        [Required]
        public decimal Price { get; set; }

        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
