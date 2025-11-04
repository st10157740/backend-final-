using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MbabaneHighlandersBackend2.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace MbabaneHighlandersBackend2.Tests
{
    public class AccountControllerTests
    {
        private readonly AccountController _controller;

        public AccountControllerTests()
        {
            // in-memory configuration for JWT
            var settings = new Dictionary<string, string>
        {
            {"Jwt:Key", "TestKey123456"},
            {"Jwt:Issuer", "TestIssuer"},
            {"Jwt:Audience", "TestAudience"}
        };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            // Use null for managers since we’re testing only validation
            _controller = new AccountController(null, null, config);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenEmailOrPasswordMissing()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "",
                Password = ""
            };

            // Act
            var result = await _controller.Login(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenEmailMissing()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "",
                Password = "Test123!",
                ConfirmPassword = "Test123!"
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenPasswordsDoNotMatch()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "user@test.com",
                Password = "12345",
                ConfirmPassword = "54321"
            };

            // Act
            var result = await _controller.Register(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }


}