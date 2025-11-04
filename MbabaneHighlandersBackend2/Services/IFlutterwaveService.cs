using MbabaneHighlandersBackend2.Model;
using System.Text.Json;
using System.Text;

namespace MbabaneHighlandersBackend2.Services
{
    public interface IFlutterwaveService
    {
        Task<string> GenerateCheckoutUrl(Member member);
    }

    public class FlutterwaveService : IFlutterwaveService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _http;

        public FlutterwaveService(IConfiguration config, HttpClient http)
        {
            _config = config;
            _http = http;
        }

        public async Task<string> GenerateCheckoutUrl(Member member)
        {
            var payload = new
            {
                tx_ref = Guid.NewGuid().ToString(),
                amount = GetAmountForTier(member.MembershipTier),
                currency = "ZAR",
                redirect_url = "http://localhost:5173/payment/complete",
                customer = new
                {
                    email = member.Email,
                    name = member.FullName
                },
                customizations = new
                {
                    title = "Mbabane Highlanders Membership",
                    description = $"{member.MembershipTier} Tier"
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.flutterwave.com/v3/payments")
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("Authorization", $"Bearer {_config["Flutterwave:SecretKey"]}");

            var response = await _http.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            Console.WriteLine("Flutterwave response status: " + response.StatusCode);
            Console.WriteLine("Flutterwave response body: " + json);

            try
            {
                var result = JsonDocument.Parse(json);

                if (result.RootElement.TryGetProperty("data", out var dataElement) &&
                    dataElement.ValueKind == JsonValueKind.Object &&
                    dataElement.TryGetProperty("link", out var linkElement))
                {
                    return linkElement.GetString();
                }

                Console.WriteLine("Missing 'data.link' in Flutterwave response.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error parsing Flutterwave response: " + ex.Message);
                return null;
            }
        }

        private int GetAmountForTier(string tier) => tier switch
        {
            "Supporter" => 150,
            "Premium" => 300,
            "VIP" => 500,
            _ => 150
        };
    }
}