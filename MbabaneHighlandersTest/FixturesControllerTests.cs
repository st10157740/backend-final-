using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MbabaneHighlandersBackend2.Controllers;
using MbabaneHighlandersBackend2.Model;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MbabaneHighlandersBackend2.Tests
{
    public class FixturesControllerTests : IDisposable
    {
        private readonly MbabaneHighlandersBackend2Context _context;
        private readonly FixturesController _controller;

        public FixturesControllerTests()
        {
            var options = new DbContextOptionsBuilder<MbabaneHighlandersBackend2Context>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new MbabaneHighlandersBackend2Context(options);

            // ✅ Use DateTime.Now.Date for DateOnly fields and fixed times for TimeOnly
            _context.Fixture.AddRange(
                new Fixture
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
                    Time = new TimeOnly(15, 0),
                    HomeTeam = "Team A",
                    AwayTeam = "Team B",
                    Stadium = "Stadium 1"
                },
                new Fixture
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(2)),
                    Time = new TimeOnly(17, 30),
                    HomeTeam = "Team C",
                    AwayTeam = "Team D",
                    Stadium = "Stadium 2"
                }
            );

            _context.SaveChanges();
            _controller = new FixturesController(_context);
        }

        [Fact]
        public async Task GetFixtures_ReturnsOk()
        {
            var result = await _controller.GetFixtures();
            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task CreateFixture_ReturnsCreatedAndAddsToDb()
        {
            var fixture = new Fixture
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(3)),
                Time = new TimeOnly(19, 0),
                HomeTeam = "Team E",
                AwayTeam = "Team F",
                Stadium = "Stadium 3"
            };

            var result = await _controller.CreateFixture(fixture);
            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(3, _context.Fixture.Count());
        }

        [Fact]
        public async Task UpdateFixture_ReturnsOk()
        {
            var existing = _context.Fixture.First();
            var updated = new Fixture
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(5)),
                Time = new TimeOnly(20, 0),
                HomeTeam = "Updated Team",
                AwayTeam = "Updated Away",
                Stadium = "Updated Stadium"
            };

            var result = await _controller.UpdateFixture(existing.Id, updated);
            var okResult = Assert.IsType<OkObjectResult>(result);

            var fromDb = await _context.Fixture.FindAsync(existing.Id);
            Assert.Equal("Updated Team", fromDb.HomeTeam);
        }

        [Fact]
        public async Task Delete_RemovesFixture_ReturnsNoContent()
        {
            var fixture = _context.Fixture.First();
            var result = await _controller.Delete(fixture.Id);

            Assert.IsType<NoContentResult>(result);
            Assert.Null(await _context.Fixture.FindAsync(fixture.Id));
        }

        [Fact]
        public async Task GetFixtureById_ReturnsOk()
        {
            var fixture = _context.Fixture.First();
            var result = await _controller.GetFixtureById(fixture.Id);

            Assert.IsType<OkObjectResult>(result);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
