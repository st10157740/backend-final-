using System;
using System.Collections.Generic;
using System.IO;
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
    public class NewsControllerTests
    {
        private readonly NewsController _controller;
        private readonly MbabaneHighlandersBackend2Context _context;
        private readonly Mock<IBlobUpload> _mockBlobUpload;

        public NewsControllerTests()
        {
            var options = new DbContextOptionsBuilder<MbabaneHighlandersBackend2Context>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new MbabaneHighlandersBackend2Context(options);

            _mockBlobUpload = new Mock<IBlobUpload>();
            _mockBlobUpload.Setup(b => b.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                           .ReturnsAsync("http://fakeurl.com/fakeimage.png");

            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Azure:NewsImageContainer"]).Returns("news-container");

            _controller = new NewsController(_context, mockConfig.Object, _mockBlobUpload.Object);
        }

        [Fact]
        public async Task GetAllNews_ReturnsOkWithEmptyList()
        {
            // Act
            var result = await _controller.GetAllNews();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var newsList = Assert.IsAssignableFrom<List<News>>(okResult.Value);
            Assert.Empty(newsList);
        }

        

        [Fact]
        public async Task AddNews_MissingTitle_ReturnsBadRequest()
        {
            var result = await _controller.AddNews("", "http://example.com", null);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Title is required.", badRequest.Value);
        }

        [Fact]
        public async Task GetNewsById_ExistingNews_ReturnsOk()
        {
            var news = new News { Title = "Existing News", CreatedUtc = DateTime.UtcNow, UpdatedUtc = DateTime.UtcNow };
            _context.News.Add(news);
            await _context.SaveChangesAsync();

            var result = await _controller.GetNewsById(news.Id);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedNews = Assert.IsType<News>(okResult.Value);
            Assert.Equal("Existing News", returnedNews.Title);
        }

        [Fact]
        public async Task GetNewsById_NonExistingNews_ReturnsNotFound()
        {
            var result = await _controller.GetNewsById(999);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("News item not found.", notFound.Value);
        }


        [Fact]
        public async Task UpdateNews_NonExistingNews_ReturnsNotFound()
        {
            var result = await _controller.UpdateNews(999, "Title", "Link", null);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("News item not found.", notFound.Value);
        }

        [Fact]
        public async Task DeleteNews_NonExistingNews_ReturnsNotFound()
        {
            var result = await _controller.DeleteNews(999);
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("News item not found.", notFound.Value);
        }
    }
}
