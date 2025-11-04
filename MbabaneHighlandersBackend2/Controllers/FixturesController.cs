using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MbabaneHighlandersBackend2.Model;
using System.Globalization;

namespace MbabaneHighlandersBackend2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FixturesController : ControllerBase
    {
        private readonly MbabaneHighlandersBackend2Context _context;

        public FixturesController(MbabaneHighlandersBackend2Context context)
        {
            _context = context;
        }

        // GET: api/Fixtures
        [HttpGet]
        public async Task<IActionResult> GetFixtures()
        {
            var fixtures = await _context.Fixture
                .OrderBy(f => f.Date).ThenBy(f => f.Time)
                .Select(f => new
                {
                    id = f.Id,
                    date = f.Date,
                    time = f.Time,
                    home = f.HomeTeam,
                    away = f.AwayTeam,
                    stadium = f.Stadium
                })
                .ToListAsync();

            return Ok(fixtures);
        }

        // POST: api/Fixtures
        [HttpPost]
        public async Task<IActionResult> CreateFixture([FromBody] Fixture fixture)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            fixture.HomeTeam = fixture.HomeTeam;
            fixture.AwayTeam = fixture.AwayTeam;
            fixture.Date = fixture.Date;
            fixture.Time = fixture.Time;
            fixture.Stadium = fixture.Stadium?.Trim() ?? string.Empty;

            _context.Fixture.Add(fixture);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFixtures), new { id = fixture.Id }, fixture);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Fixture.FindAsync(id);
            if (entity is null) return NotFound();

            _context.Fixture.Remove(entity);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/Fixtures/update-fixture/{id}
        [HttpPut("update-fixture/{id}")]
        public async Task<IActionResult> UpdateFixture(int id, [FromBody] Fixture updatedFixture)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var fixture = await _context.Fixture.FindAsync(id);
            if (fixture == null)
                return NotFound("Fixture not found.");

            fixture.Date = updatedFixture.Date;
            fixture.Time = updatedFixture.Time;
            fixture.HomeTeam = updatedFixture.HomeTeam;
            fixture.AwayTeam = updatedFixture.AwayTeam;
            fixture.Stadium = updatedFixture.Stadium?.Trim() ?? string.Empty;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Fixture updated successfully.", fixture });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating fixture: {ex.Message}");
                return StatusCode(500, "An error occurred while updating the fixture.");
            }
        }

        // GET: api/fixtures/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFixtureById(int id)
        {
            var fixture = await _context.Fixture.FindAsync(id);
            if (fixture == null)
                return NotFound("Fixture not found.");

            return Ok(new
            {
                id = fixture.Id,
                date = fixture.Date.ToString("yyyy-MM-dd"),
                time = fixture.Time.ToString("HH:mm"),
                homeTeam = fixture.HomeTeam,
                awayTeam = fixture.AwayTeam,
                stadium = fixture.Stadium
            });
        }

    }
}
