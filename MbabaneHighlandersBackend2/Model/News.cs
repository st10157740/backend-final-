using System.ComponentModel.DataAnnotations;

namespace MbabaneHighlandersBackend2.Model
{
    public class News
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Url]
        public string? Link { get; set; }
        public string? ImageFileName { get; set; }

        public int Likes { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedUtc { get; set; }
    }
}
