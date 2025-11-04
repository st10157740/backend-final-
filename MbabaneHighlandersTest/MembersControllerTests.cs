using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MbabaneHighlandersBackend2.Controllers;
using MbabaneHighlandersBackend2.Model;
using MbabaneHighlandersBackend2.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace MbabaneHighlandersBackend2.Tests
{
    public class MembersControllerTests
    {
        private readonly DbContextOptions<MbabaneHighlandersBackend2Context> _dbOptions;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IEmailService> _mockEmailService;

        public MembersControllerTests()
        {
            _dbOptions = new DbContextOptionsBuilder<MbabaneHighlandersBackend2Context>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _mockConfig = new Mock<IConfiguration>();
            _mockEmailService = new Mock<IEmailService>();

            // Set mock configuration keys
            _mockConfig.Setup(x => x["Azure:StorageConnectionString"]).Returns("UseDevelopmentStorage=true");
            _mockConfig.Setup(x => x["Azure:ContainerName"]).Returns("test-container");
            _mockConfig.Setup(x => x["Azure:CardContainer"]).Returns("test-cards");
            _mockConfig.Setup(x => x["PayFast:MerchantId"]).Returns("10000100");
            _mockConfig.Setup(x => x["PayFast:MerchantKey"]).Returns("46f0cd694581a");
            _mockConfig.Setup(x => x["PayFast:UseSandbox"]).Returns("true");
            _mockConfig.Setup(x => x["PayFast:SandboxUrl"]).Returns("https://sandbox.payfast.co.za/eng/process");
            _mockConfig.Setup(x => x["Resend:ApiKey"]).Returns("dummykey");
        }

        private MembersController CreateController()
        {
            var context = new MbabaneHighlandersBackend2Context(_dbOptions);
            return new MembersController(context, _mockConfig.Object, _mockEmailService.Object);
        }

        [Fact]
        public async Task GetMembers_ReturnsOkWithEmptyList_WhenNoMembers()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.GetMembers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var members = Assert.IsType<List<Member>>(okResult.Value);
            Assert.Empty(members);
        }

        [Fact]
        public async Task RegisterMember_ReturnsBadRequest_WhenFullNameMissing()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.RegisterMember(
                FullName: "",
                Email: "test@example.com",
                PhoneNumber: "12345",
                Branch: "Main",
                MembershipTier: "Supporter",
                ProofOfPayment: null
            );

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Missing required member information.", badRequest.Value);
        }

        [Fact]
        public async Task RegisterMember_ReturnsBadRequest_WhenInvalidTier()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.RegisterMember(
                FullName: "John Doe",
                Email: "john@example.com",
                PhoneNumber: "12345",
                Branch: "Main",
                MembershipTier: "Ultra",
                ProofOfPayment: null
            );

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid membership tier.", badRequest.Value);
        }


        [Fact]
        public async Task UpdatePaymentStatus_ReturnsNotFound_WhenMemberNotExists()
        {
            // Arrange
            var controller = CreateController();
            string fakeId = Guid.NewGuid().ToString();

            // Act
            var result = await controller.UpdatePaymentStatus(fakeId, "Accepted");

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Member not found.", notFound.Value);
        }

        [Fact]
        public void GenerateMembershipCard_ReturnsBitmap_WithExpectedSize()
        {
            // Arrange
            var member = new Member
            {
                FullName = "John Doe",
                MemberCode = "MBH-001",
                MembershipTier = "Supporter",
                JoinedAt = DateTime.UtcNow
            };

            // Act
            var card = MembersController.GenerateMembershipCard(member);

            // Assert
            Assert.NotNull(card);
            Assert.Equal(800, card.Width);
            Assert.Equal(400, card.Height);
        }

        [Fact]
        public async Task PayFastNotify_ReturnsBadRequest_WhenMissingMemberCode()
        {
            // Arrange
            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            controller.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "payment_status", "COMPLETE" },
                { "item_name", "" }
            });

            // Act
            var result = await controller.PayFastNotify();

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Missing member code.", badRequest.Value);
        }
    }
}
