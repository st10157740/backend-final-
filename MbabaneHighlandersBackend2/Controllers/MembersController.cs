using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MbabaneHighlandersBackend2.Model;
using MbabaneHighlandersBackend2.Services;
using Azure.Storage.Blobs;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;
using Resend;

namespace MbabaneHighlandersBackend2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembersController : ControllerBase
    {
        private readonly MbabaneHighlandersBackend2Context _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;


        public MembersController(MbabaneHighlandersBackend2Context context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        private async Task<string> UploadCardToBlobAsync(Bitmap cardImage, string fileName)
        {
            using var stream = new MemoryStream();
            cardImage.Save(stream, ImageFormat.Png);
            stream.Position = 0;

            var connectionString = _config["Azure:StorageConnectionString"];
            var containerName = _config["Azure:CardContainer"];

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(stream, overwrite: true);

            return blobClient.Uri.ToString();
        }

        // ✅ GET: api/Members
        [HttpGet]
        public async Task<IActionResult> GetMembers()
        {
            var members = await _context.Member.ToListAsync();
            return Ok(members);
        }


        [HttpPost]
        public async Task<IActionResult> RegisterMember(
    [FromForm] string FullName,
    [FromForm] string Email,
    [FromForm] string PhoneNumber,
    [FromForm] string Branch,
    [FromForm] string MembershipTier,
    [FromForm] IFormFile? ProofOfPayment)
        {
            Console.WriteLine($"FullName: {FullName}");
            Console.WriteLine($"Email: {Email}");
            Console.WriteLine($"PhoneNumber: {PhoneNumber}");
            Console.WriteLine($"Branch: {Branch}");
            Console.WriteLine($"MembershipTier: {MembershipTier}");
            Console.WriteLine($"File: {ProofOfPayment?.FileName}");

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email))
            {
                return BadRequest("Missing required member information.");
            }

            string uploadedFileName = "NoFile";
            int amount = 0;

            if (ProofOfPayment != null && ProofOfPayment.Length > 0)
            {
                try
                {
                    var connectionString = _config["Azure:StorageConnectionString"];
                    var containerName = _config["Azure:ContainerName"];

                    var blobServiceClient = new BlobServiceClient(connectionString);
                    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                    await containerClient.CreateIfNotExistsAsync();

                    uploadedFileName = $"{Guid.NewGuid()}_{ProofOfPayment.FileName}";
                    var blobClient = containerClient.GetBlobClient(uploadedFileName);

                    using var stream = ProofOfPayment.OpenReadStream();
                    await blobClient.UploadAsync(stream, overwrite: true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Blob upload failed: {ex.Message}");
                    return StatusCode(500, "File upload failed.");
                }
            }

            switch (MembershipTier?.Trim().ToLower())
            {
                case "supporter":
                    amount = 50;
                    break;
                case "premium":
                    amount = 100;
                    break;
                case "vip":
                    amount = 150;
                    break;
                default:
                    return BadRequest("Invalid membership tier.");
            }

            // Generate next MemberCode
            var lastMember = await _context.Member
                .Where(m => m.MemberCode != null)
                .OrderByDescending(m => m.MemberCode)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastMember != null)
            {
                var match = Regex.Match(lastMember.MemberCode, @"MBH-(\d+)");
                if (match.Success)
                {
                    nextNumber = int.Parse(match.Groups[1].Value) + 1;
                }
            }

            string newMemberCode = $"MBH-{nextNumber.ToString("D3")}";


            var newMember = new Member
            {
                MemberID = Guid.NewGuid().ToString(),
                MemberCode = newMemberCode,
                FullName = FullName,
                Email = Email,
                PhoneNumber = PhoneNumber,
                Branch = Branch,
                MembershipTier = MembershipTier,
                Amount = amount,
                FileUpload = uploadedFileName,
                JoinedAt = DateTime.UtcNow
            };

            try
            {
                _context.Member.Add(newMember);
                await _context.SaveChangesAsync();

                var data = new Dictionary<string, string>
                {
                    { "merchant_id", _config["PayFast:MerchantId"] },
                    { "merchant_key", _config["PayFast:MerchantKey"] },
                    { "return_url", "https://mbabanehighlandersam.co.sz/success" },
                    { "cancel_url", "https://mbabanehighlandersam.co.sz/failed" },
                    { "notify_url", "https://mbabane-defrdwe3dkekfkdp.southafricanorth-01.azurewebsites.net/api/members/notify" },
                    { "amount", newMember.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) },
                    { "item_name", $"Member #{newMember.MemberCode}" },
                    { "name_first", newMember.FullName },
                    { "email_address", newMember.Email }
                };

                var query = string.Join("&", data.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var useSandbox = bool.Parse(_config["Payfast:UseSandbox"]);
                var baseUrl = useSandbox
                    ? _config["Payfast:SandboxUrl"]
                    : _config["Payfast:LiveUrl"];

                var redirectUrl = $"{baseUrl}?{query}";

                Console.WriteLine("Final PayFast Redirect URL:");
                Console.WriteLine(redirectUrl);

                return Ok(new { message = "Member registered successfully.", redirectUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving member: {ex.Message}");
                return StatusCode(500, "An error occurred while saving member data.");
            }
        }

        private bool MemberExists(string id)
        {
            return _context.Member.Any(e => e.MemberID == id);
        }
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdatePaymentStatus(string id, [FromForm] string status)
        {
            try
            {
                var member = await _context.Member.FindAsync(id);
                if (member == null)
                    return NotFound("Member not found.");

                member.PaymentStatus = status;

                if (status.Equals("Accepted", StringComparison.OrdinalIgnoreCase))
                {
                    var cardImage = GenerateMembershipCard(member);
                    var fileName = $"Card_{member.MemberID}.png";
                    var cardUrl = await UploadCardToBlobAsync(cardImage, fileName);
                    member.CardUrl = cardUrl;
                }

                await _context.SaveChangesAsync();
                await _emailService.SendMembershipCard(member);
                return Ok(new { message = $"Payment status updated to {status}.", member.CardUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Update failed: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner: {ex.InnerException.Message}");
                return StatusCode(500, "Failed to update payment status.");
            }
        }
        public static Bitmap GenerateMembershipCard(Member member)
        {
            int width = 800;
            int height = 400;
            var card = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(card);

            // High-quality rendering
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Rounded corners
            var path = new GraphicsPath();
            int radius = 30;
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(width - radius, 0, radius, radius, 270, 90);
            path.AddArc(width - radius, height - radius, radius, radius, 0, 90);
            path.AddArc(0, height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            graphics.SetClip(path);

            // Background
            graphics.Clear(Color.Black);

            // Gold color
            var goldBrush = new SolidBrush(ColorTranslator.FromHtml("#C9A24B"));

            // Fonts
            var fontTitle = new Font("Arial", 24, FontStyle.Bold);
            var fontBody = new Font("Arial", 14, FontStyle.Regular);

            // Logo
            string logoPath = Path.Combine("Logo", "MbabaneLogo.jpg"); // adjust filename if needed
            using var logo = Image.FromFile(logoPath);
            graphics.DrawImage(logo, new Rectangle(20, 0, 180, 180));

            // Text layout
            float x = 220;
            graphics.DrawString("MBABANE HIGHLANDERS AM", fontTitle, goldBrush, new PointF(x, 30));
            graphics.DrawString("Football Club | Est. 1952", fontBody, goldBrush, new PointF(x, 70));
            graphics.DrawString("Ezimnyama Ngenkani – BLACK BULL", fontBody, goldBrush, new PointF(x, 100));

            graphics.DrawString(member.FullName.ToUpper(), fontTitle, goldBrush, new PointF(x, 160));
            graphics.DrawString($"MEMBER CODE: {member.MemberCode}", fontBody, goldBrush, new PointF(x, 200));
            graphics.DrawString($"MEMBERSHIP: {member.MembershipTier}", fontBody, goldBrush, new PointF(x, 230));
            graphics.DrawString($"JOINED: {member.JoinedAt:dd MMMM yyyy}", fontBody, goldBrush, new PointF(x, 260));

            graphics.DrawString("membership@mbabanehighlanders.com", fontBody, goldBrush, new PointF(x, 330));
            graphics.DrawString("www.mbabanehighlanders.com", fontBody, goldBrush, new PointF(x, 360));

            return card;
        }

        [HttpPost("notify")]
        public async Task<IActionResult> PayFastNotify()
        {
            IResend resend = ResendClient.Create(_config["Resend:ApiKey"]);
            try
            {
                Console.WriteLine("📩 PayFast notification received...");
                var form = await Request.ReadFormAsync();
                var paymentStatus = form["payment_status"];
                var memberCode = form["item_name"].ToString()
                    .Replace("Member #", "")
                    .Replace("Member", "")
                    .Trim();

                if (string.IsNullOrEmpty(memberCode))
                    return BadRequest("Missing member code.");

                var member = await _context.Member
                    .FirstOrDefaultAsync(m => m.MemberCode == memberCode);

                if (member == null)
                    return NotFound("Member not found.");

                if (paymentStatus == "COMPLETE")
                {
                    member.PaymentStatus = "Accepted";
                    await _context.SaveChangesAsync();

                    var cardImage = GenerateMembershipCard(member);
                    var fileName = $"Card_{member.MemberID}.png";
                    var cardUrl = await UploadCardToBlobAsync(cardImage, fileName);
                    member.CardUrl = cardUrl;

                    await _emailService.SendMembershipCard(member);
                    Console.WriteLine($"✅ Member {member.MemberCode} marked as PAID.");
                }
                else
                {
                    Console.WriteLine($"⚠️ Payment status for Member {member.MemberCode}: {paymentStatus}");
                }


                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ PayFast ITN error: " + ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine("Inner Exception: " + ex.InnerException.Message);
                return StatusCode(500, "Internal server error");
            }
        }



    }
}