using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MbabaneHighlandersBackend2.Model
{
    public class Member
    {
        [BindNever]
        public string MemberID { get; set; } = Guid.NewGuid().ToString();
        [BindNever]
        public string MemberCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Branch { get; set; }
        public string MembershipTier { get; set; }
        [BindNever]
        public int Amount { get; set; } = 0;

        public string FileUpload { get; set; } = "No file";
        [BindNever]
        public string PaymentStatus { get; set; } = "Under Review";
        [BindNever]
        public string CardUrl { get; set; } = "pending";

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
