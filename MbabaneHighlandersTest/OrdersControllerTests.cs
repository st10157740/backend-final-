using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MbabaneHighlandersBackend2.Controllers;
using MbabaneHighlandersBackend2.Model;
using MbabaneHighlandersBackend2.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace MbabaneHighlandersBackend2.Tests
{
    public class OrdersControllerTests
    {
        private readonly OrdersController _controller;
        private readonly MbabaneHighlandersBackend2Context _context;
        private readonly Mock<IEmailService> _mockEmailService;

        public OrdersControllerTests()
        {
            var options = new DbContextOptionsBuilder<MbabaneHighlandersBackend2Context>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new MbabaneHighlandersBackend2Context(options);

            _mockEmailService = new Mock<IEmailService>();
            var mockConfig = new Mock<IConfiguration>();

            _controller = new OrdersController(_context, mockConfig.Object, _mockEmailService.Object);
        }

        [Fact]
        public async Task InitiatePayment_InvalidOrder_ReturnsBadRequest()
        {
            var result = await _controller.InitiatePayment(new OrdersController.OrderDto { Items = new List<OrdersController.OrderItemDto>() });
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid order payload.", badRequest.Value);
        }

        [Fact]
        public async Task GetAllOrders_ReturnsOkWithOrders()
        {
            var order = new Order
            {
                OrderCode = "ORD12345",
                CustomerFullName = "Jane Doe",
                CustomerEmail = "jane@example.com",
                ShippingAddress = "123 Street",
                Status = "Pending",
                CreatedUtc = DateTime.UtcNow
            };
            _context.Order.Add(order);
            await _context.SaveChangesAsync();

            var result = await _controller.GetAllOrders();
            var okResult = Assert.IsType<OkObjectResult>(result);
            var orders = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Single(orders);
        }

        [Fact]
        public async Task UpdateOrderStatus_NonExistingOrder_ReturnsNotFound()
        {
            var dto = new OrdersController.StatusUpdateDto { Status = "Accepted" };
            var result = await _controller.UpdateOrderStatus(999, dto);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Order not found.", notFound.Value);
        }
    }
}
