using MbabaneHighlandersBackend2.Model;
using Resend;

namespace MbabaneHighlandersBackend2.Services
{
    public class SendEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public SendEmailService(IConfiguration config) 
        {
            _config = config;
        }

        public async Task SendMembershipCard(Member member)
        {
            var resend = ResendClient.Create(_config["Resend:ApiKey"]);

            await resend.EmailSendAsync(new EmailMessage
            {
                From = "Mbabane Highlanders <noreply@mbabanehighlandersam.co.sz>",
                To = $"{member.Email}",
                Subject = "Membership card",
                HtmlBody = $"<p>Thank you {member.FullName} for signing up for the Mbabane Highlanders membership. Attached is a link to download your membership card.</p>" +
                $"<a href = '{member.CardUrl}'>Click here to download your membership card</a>"
            });
        }

        public async Task SendOrderConfirmation(Order order)
        {
            var resend = ResendClient.Create(_config["Resend:ApiKey"]);

            var itemList = string.Join("<br/>", order.Items.Select(item =>
                $"- {item.ProductName} (Size: {item.Size}, Qty: {item.Quantity}) — E{item.LineTotal:F2}"
            ));

            var html = $@"
        <p>Dear {order.CustomerFullName},</p>
        <p>Thank you for your order with <strong>Mbabane Highlanders</strong>! Your payment has been received and your order is now being processed.</p>
        <p><strong>Order Code:</strong> {order.OrderCode}</p>
        <p><strong>Shipping Address:</strong><br/>{order.ShippingAddress}</p>
        <p><strong>Order Summary:</strong><br/>{itemList}</p>
        <p><strong>Total Paid:</strong> E{order.TotalAmount:F2}</p>
        <p>We’ll notify you once your order ships. If you have any questions, feel free to reply to this email.</p>
        <p>Highlanders forever,<br/>Mbabane Highlanders Store Team</p>
    ";

            await resend.EmailSendAsync(new EmailMessage
            {
                From = "Mbabane Highlanders <noreply@mbabanehighlandersam.co.sz>",
                To = $"{order.CustomerEmail}",
                Subject = $"Order Confirmation – {order.OrderCode}",
                HtmlBody = html
            });
        }
    }
}
