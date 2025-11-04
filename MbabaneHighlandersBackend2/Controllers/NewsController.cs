using System;
using System.Linq;
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
    public class NewsController : ControllerBase
    {
        private readonly MbabaneHighlandersBackend2Context _context;
        private readonly IBlobUpload _blobUpload;
        private readonly string _newsImageContainer;

        public NewsController(MbabaneHighlandersBackend2Context context, IConfiguration config, IBlobUpload blobUpload)
        {
            _context = context;
            _blobUpload = blobUpload;
            _newsImageContainer = config["Azure:NewsImageContainer"];
        }

        // GET: api/news
        [HttpGet]
        public async Task<IActionResult> GetAllNews()
        {
            var newsList = await _context.News
                .OrderByDescending(n => n.CreatedUtc)
                .ToListAsync();

            return Ok(newsList);
        }

        // GET: api/news/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetNewsById(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
                return NotFound("News item not found.");

            return Ok(news);
        }

        // POST: api/news/add
        [HttpPost("add")]
        public async Task<IActionResult> AddNews(
            [FromForm] string Title,
            [FromForm] string Link,
            [FromForm] IFormFile ImageFile)
        {
            if (string.IsNullOrWhiteSpace(Title))
                return BadRequest("Title is required.");

            string imageUrl = null;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                try
                {
                    imageUrl = await _blobUpload.UploadFileAsync(ImageFile, _newsImageContainer);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Image upload failed: {ex.Message}");
                    return StatusCode(500, "Image upload failed.");
                }
            }

            var news = new News
            {
                Title = Title,
                Link = Link,
                ImageFileName = imageUrl,
                Likes = 0,
                CreatedUtc = DateTime.UtcNow,
                UpdatedUtc = DateTime.UtcNow
            };

            try
            {
                _context.News.Add(news);
                await _context.SaveChangesAsync();
                return Ok(new { message = "News added successfully.", news });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving news: {ex.Message}");
                return StatusCode(500, "Failed to save news.");
            }
        }

        // PUT: api/news/update/{id}
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateNews(
            int id,
            [FromForm] string Title,
            [FromForm] string Link,
            [FromForm] IFormFile? ImageFile) // Make image optional
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
                return NotFound("News item not found.");

            news.Title = string.IsNullOrWhiteSpace(Title) ? news.Title : Title;
            news.Link = string.IsNullOrWhiteSpace(Link) ? news.Link : Link;
            news.UpdatedUtc = DateTime.UtcNow;

            if (ImageFile != null && ImageFile.Length > 0)
            {
                try
                {
                    var newImageUrl = await _blobUpload.UploadFileAsync(ImageFile, _newsImageContainer);
                    news.ImageFileName = newImageUrl;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Image upload failed: {ex.Message}");
                    return StatusCode(500, "Image upload failed.");
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "News updated successfully.", news });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating news: {ex.Message}");
                return StatusCode(500, "Failed to update news.");
            }
        }

        // DELETE: api/news/delete/{id}
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteNews(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null)
                return NotFound("News item not found.");

            _context.News.Remove(news);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "News deleted successfully." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting news: {ex.Message}");
                return StatusCode(500, "Failed to delete news.");
            }
        }
    }
}