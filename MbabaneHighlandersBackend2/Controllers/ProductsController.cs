using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MbabaneHighlandersBackend2.Model;
using MbabaneHighlandersBackend2.Services;
using Microsoft.AspNetCore.Http;

namespace MbabaneHighlandersBackend2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly MbabaneHighlandersBackend2Context _context;
        private readonly IBlobUpload _blobUpload;
        private readonly string _productContainer;

        public ProductsController(MbabaneHighlandersBackend2Context context, IConfiguration config, IBlobUpload blobUpload)
        {
            _context = context;
            _blobUpload = blobUpload;
            _productContainer = config["Azure:ProductContainer"];
        }

        // POST: api/products/add-product
        [HttpPost("add-product")]
        public async Task<IActionResult> AddProduct(
            [FromForm] string Name,
            [FromForm] string Color,
            [FromForm] decimal Price,
            [FromForm] IFormFile ImageFile)
        {
            if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Color) || Price <= 0)
                return BadRequest("Missing or invalid product information.");

            string uploadedImageUrl = null;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                try
                {
                    uploadedImageUrl = await _blobUpload.UploadFileAsync(ImageFile, _productContainer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Blob upload failed: {ex.Message}");
                    return StatusCode(500, "Image upload failed.");
                }
            }

            var newProduct = new Product
            {
                Name = Name,
                Color = Color,
                Price = Price,
                ImageUrl = uploadedImageUrl,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow
            };

            try
            {
                _context.Product.Add(newProduct);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Product added successfully.", product = newProduct });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving product: {ex.Message}");
                return StatusCode(500, "An error occurred while saving product data.");
            }
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _context.Product
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedUtc)
                .ToListAsync();

            return Ok(products);
        }

        // PUT: api/products/update-product/{id}
        [HttpPut("update-product/{id}")]
        public async Task<IActionResult> UpdateProduct(
            int id,
            [FromForm] string Name,
            [FromForm] string Color,
            [FromForm] decimal Price,
            [FromForm] IFormFile? ImageFile) // Make image optional
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
                return NotFound("Product not found.");

            // Update basic fields
            product.Name = string.IsNullOrWhiteSpace(Name) ? product.Name : Name;
            product.Color = string.IsNullOrWhiteSpace(Color) ? product.Color : Color;
            product.Price = Price > 0 ? Price : product.Price;

            // Only update image if a new one is provided
            if (ImageFile != null && ImageFile.Length > 0)
            {
                try
                {
                    var newImageUrl = await _blobUpload.UploadFileAsync(ImageFile, _productContainer);
                    product.ImageUrl = newImageUrl;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Blob upload failed: {ex.Message}");
                    return StatusCode(500, "Image upload failed.");
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Product updated successfully.", product });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating product: {ex.Message}");
                return StatusCode(500, "An error occurred while updating product data.");
            }
        }

        // DELETE: api/products/delete-product/{id}
        [HttpDelete("delete-product/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
                return NotFound("Product not found.");

            product.IsActive = false;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Product deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting product: {ex.Message}");
                return StatusCode(500, "An error occurred while deleting product.");
            }
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _context.Product.FindAsync(id);
            if (product == null)
                return NotFound("Product not found.");

            return Ok(new
            {
                id = product.Id,
                name = product.Name,
                color = product.Color,
                price = product.Price,
                imageUrl = product.ImageUrl,
                isActive = product.IsActive,
                createdUtc = product.CreatedUtc
            });
        }
    }
}