namespace MbabaneHighlandersBackend2.Model
{
    public class Contact
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }

        public string PhoneNumber { get; set; }
        public string InquiryType { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }

        public bool IsResolved { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    }
}
