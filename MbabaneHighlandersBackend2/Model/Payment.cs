namespace MbabaneHighlandersBackend2.Model
{
    public class Payment
    {
        public string PaymentID { get; set; } = Guid.NewGuid().ToString();
        public string MemberID { get; set; }
        public string PaymentGateway { get; set; }
        public int Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
