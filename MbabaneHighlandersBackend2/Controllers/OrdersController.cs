using Microsoft.AspNetCore.Mvc;
using MbabaneHighlandersBackend2.Model;
using Microsoft.EntityFrameworkCore;
using System.Web;
using System.Globalization;
using MbabaneHighlandersBackend2.Services;

namespace MbabaneHighlandersBackend2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly MbabaneHighlandersBackend2Context _context;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public OrdersController(MbabaneHighlandersBackend2Context context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService;
        }

        // POST: api/orders/initiate-payment
        [HttpPost("initiate-payment")]
        public async Task<IActionResult> InitiatePayment([FromBody] OrderDto dto)
        {
            if (dto == null || dto.Items == null || !dto.Items.Any())
                return BadRequest("Invalid order payload.");

            var order = new Order
            {
                OrderCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CustomerFullName = dto.CustomerFullName,
                CustomerEmail = dto.CustomerEmail,
                CustomerPhone = dto.CustomerPhone,
                ShippingAddress = dto.ShippingAddress,
                Status = "Pending",
                CreatedUtc = DateTime.UtcNow
            };

            foreach (var item in dto.Items)
            {
                var product = await _context.Product.FindAsync(item.ProductId);
                if (product == null) continue;

                var lineTotal = item.UnitPrice * item.Quantity;

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Color = item.Color,
                    Size = item.Size,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    LineTotal = lineTotal
                });
            }

            order.TotalAmount = order.Items.Sum(i => i.LineTotal);

            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            // PayFast sandbox credentials
            var merchantId = "10000100";
            var merchantKey = "46f0cd694581a";
            var returnUrl = "https://mbabanehighlandersam.co.sz/success";
            var cancelUrl = "https://mbabanehighlandersam.co.sz/failed";
            var notifyUrl = "https://mbabane-defrdwe3dkekfkdp.southafricanorth-01.azurewebsites.net/api/orders/payfast-notify";

            // Build PayFast redirect URL
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["merchant_id"] = merchantId;
            query["merchant_key"] = merchantKey;
            query["return_url"] = returnUrl;
            query["cancel_url"] = cancelUrl;
            query["notify_url"] = notifyUrl;
            query["amount"] = order.TotalAmount.ToString("F2", CultureInfo.InvariantCulture);
            query["item_name"] = $"Order {order.OrderCode}";
            query["email_address"] = order.CustomerEmail;

            var redirectUrl = $"https://sandbox.payfast.co.za/eng/process?{query}";

            return Ok(new
            {
                message = "Order created. Redirect to PayFast.",
                orderId = order.Id,
                redirectUrl
            });
        }

        [HttpPost("payfast-notify")]
        public async Task<IActionResult> PayFastNotify()
        {
            try
            {
                // Read form data
                var form = await Request.ReadFormAsync();
                var paymentStatus = form["payment_status"];
                var orderCode = form["item_name"].ToString().Replace("Order ", "").Trim();

                if (string.IsNullOrEmpty(orderCode))
                    return BadRequest("Missing order code.");

                var order = await _context.Order
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.OrderCode == orderCode);

                if (order == null)
                    return NotFound("Order not found.");

                if (paymentStatus == "COMPLETE")
                {
                    order.Status = "Paid";
                    await _context.SaveChangesAsync();

                    await _emailService.SendOrderConfirmation(order);

                    // Optional: send confirmation email or log
                    Console.WriteLine($"Order {order.OrderCode} marked as PAID.");
                }
                else
                {
                    Console.WriteLine($"Payment status for Order {order.OrderCode}: {paymentStatus}");
                }

                return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ PayFast ITN error: " + ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }

        // GET: api/orders
        [HttpGet]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _context.Order
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedUtc)
                .ToListAsync();

            var result = orders.Select(o => new
            {
                o.Id,
                o.OrderCode,
                o.CustomerFullName,
                o.CustomerEmail,
                o.CustomerPhone,
                o.ShippingAddress,
                o.Status,
                o.TotalAmount,
                o.CreatedUtc,
                Items = o.Items.Select(i => new
                {
                    i.ProductName,
                    i.Color,
                    i.Size,
                    i.Quantity,
                    i.UnitPrice,
                    i.LineTotal
                })
            });

            return Ok(result);
        }

        // PUT: api/orders/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] StatusUpdateDto dto)
        {
            var order = await _context.Order.FindAsync(id);
            if (order == null)
                return NotFound("Order not found.");

            order.Status = dto.Status;
            await _context.SaveChangesAsync();

            if (order.Status.Equals("Accepted"))
            {
                await _emailService.SendOrderConfirmation(order);
            }

            Console.WriteLine($"Order {order.OrderCode} updated to status: {dto.Status}");

            return Ok(new
            {
                message = $"Order status updated to {dto.Status}",
                orderId = order.Id,
                newStatus = order.Status
            });
        }

        public class StatusUpdateDto
        {
            public string Status { get; set; } = "Pending";
        }

        public class OrderDto
        {
            public string CustomerFullName { get; set; } = string.Empty;
            public string CustomerEmail { get; set; } = string.Empty;
            public string? CustomerPhone { get; set; }
            public string ShippingAddress { get; set; } = string.Empty;
            public List<OrderItemDto> Items { get; set; } = new();
        }

        public class OrderItemDto
        {
            public int ProductId { get; set; }
            public string Color { get; set; } = string.Empty;
            public string Size { get; set; } = "M";
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
        }
    }
}