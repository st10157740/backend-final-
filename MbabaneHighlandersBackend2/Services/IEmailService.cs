using MbabaneHighlandersBackend2.Model;

namespace MbabaneHighlandersBackend2.Services
{
    public interface IEmailService
    {
        Task SendMembershipCard(Member member);
        Task SendOrderConfirmation(Order order);
    }
}
