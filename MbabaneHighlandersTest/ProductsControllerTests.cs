using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using MbabaneHighlandersBackend2.Controllers;
using MbabaneHighlandersBackend2.Model;
using MbabaneHighlandersBackend2.Services;

namespace MbabaneHighlandersBackend2.Tests
{
    public class ProductsControllerTests
    {
        private readonly DbContextOptions<MbabaneHighlandersBackend2Context> _dbOptions;
        private readonly Mock<IBlobUpload> _mockBlobUpload;
        private readonly IConfiguration _config;

        public ProductsControllerTests()
        {
            _dbOptions = new DbContextOptionsBuilder<MbabaneHighlandersBackend2Context>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _mockBlobUpload = new Mock<IBlobUpload>();

            var inMemorySettings = new Dictionary<string, string>
            {
                {"Azure:ProductContainer", "products"}
            };
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private IFormFile CreateDummyFormFile(string fileName = "test.jpg")
        {
            var content = new byte[] { 0x20 }; // dummy byte
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "file", fileName);
        }

        [Fact]
        public async Task AddProduct_ValidData_ReturnsOk()
        {
            using var context = new MbabaneHighlandersBackend2Context(_dbOptions);
            _mockBlobUpload.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("https://dummyurl.com/image.jpg");

            var controller = new ProductsController(context, _config, _mockBlobUpload.Object);

            var result = await controller.AddProduct(
                Name: "Bike",
                Color: "Red",
                Price: 100,
                ImageFile: CreateDummyFormFile()
            );

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var product = context.Product.FirstOrDefault();
            Assert.NotNull(product);
            Assert.Equal("Bike", product.Name);
            Assert.Equal("Red", product.Color);
            Assert.Equal(100, product.Price);
            Assert.Equal("https://dummyurl.com/image.jpg", product.ImageUrl);
        }

        [Fact]
        public async Task GetAllProducts_ReturnsOk()
        {
            using var context = new MbabaneHighlandersBackend2Context(_dbOptions);
            context.Product.Add(new Product { Name = "Bike", Color = "Blue", Price = 50, IsActive = true, CreatedUtc = DateTime.UtcNow });
            context.Product.Add(new Product { Name = "Helmet", Color = "Black", Price = 20, IsActive = false, CreatedUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var controller = new ProductsController(context, _config, _mockBlobUpload.Object);

            var result = await controller.GetAllProducts();
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var products = Assert.IsType<List<Product>>(okResult.Value);
            Assert.Single(products); // only active products
        }

        [Fact]
        public async Task UpdateProduct_ValidData_ReturnsOk()
        {
            using var context = new MbabaneHighlandersBackend2Context(_dbOptions);
            var product = new Product { Name = "Bike", Color = "Blue", Price = 50, IsActive = true, CreatedUtc = DateTime.UtcNow };
            context.Product.Add(product);
            await context.SaveChangesAsync();

            _mockBlobUpload.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("https://dummyurl.com/newimage.jpg");

            var controller = new ProductsController(context, _config, _mockBlobUpload.Object);

            var result = await controller.UpdateProduct(
                product.Id,
                Name: "Mountain Bike",
                Color: "Red",
                Price: 100,
                ImageFile: CreateDummyFormFile()
            );

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var updatedProduct = context.Product.First();
            Assert.Equal("Mountain Bike", updatedProduct.Name);
            Assert.Equal("Red", updatedProduct.Color);
            Assert.Equal(100, updatedProduct.Price);
            Assert.Equal("https://dummyurl.com/newimage.jpg", updatedProduct.ImageUrl);
        }

        [Fact]
        public async Task DeleteProduct_ValidId_ReturnsOk()
        {
            using var context = new MbabaneHighlandersBackend2Context(_dbOptions);
            var product = new Product { Name = "Bike", Color = "Blue", Price = 50, IsActive = true, CreatedUtc = DateTime.UtcNow };
            context.Product.Add(product);
            await context.SaveChangesAsync();

            var controller = new ProductsController(context, _config, _mockBlobUpload.Object);
            var result = await controller.DeleteProduct(product.Id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);

            var deletedProduct = context.Product.First();
            Assert.False(deletedProduct.IsActive);
        }

        }
    }

