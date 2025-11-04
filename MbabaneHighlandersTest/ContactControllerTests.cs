using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MbabaneHighlandersBackend2.Controllers;
using MbabaneHighlandersBackend2.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MbabaneHighlandersBackend2.Tests
{
    public class ContactsControllerTests
    {
        private readonly MbabaneHighlandersBackend2Context _context;
        private readonly ContactsController _controller;

        public ContactsControllerTests()
        {
            // ✅ Use unique in-memory database per test run
            var options = new DbContextOptionsBuilder<MbabaneHighlandersBackend2Context>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            _context = new MbabaneHighlandersBackend2Context(options);

            // Seed sample data
            _context.Contact.AddRange(
                new Contact
                {
                    Id = 1,
                    Subject = "John Doe",
                    Email = "john@example.com",
                    Message = "Hello there",
                    FullName = "Johnathan Doe",
                    InquiryType = "General",
                    PhoneNumber = "1234567890"
                },
                new Contact
                {
                    Id = 2,
                    Subject = "Jane Smith",
                    Email = "jane@example.com",
                    Message = "Need info",
                    FullName = "Jane Smith",
                    InquiryType = "Support",
                    PhoneNumber = "0987654321"
                }
            );
            _context.SaveChanges();

            _controller = new ContactsController(_context);
        }

        [Fact]
        public async Task GetContacts_ReturnsAllContacts()
        {
            var result = await _controller.GetContacts();
            var contacts = Assert.IsType<ActionResult<IEnumerable<Contact>>>(result);
            var list = Assert.IsType<List<Contact>>(contacts.Value);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetContact_ReturnsContact_WhenExists()
        {
            var result = await _controller.GetContact(1);
            var actionResult = Assert.IsType<ActionResult<Contact>>(result);
            var contact = Assert.IsType<Contact>(actionResult.Value);
            Assert.Equal("John Doe", contact.Subject);
            Assert.Equal("Johnathan Doe", contact.FullName);
            Assert.Equal("General", contact.InquiryType);
            Assert.Equal("1234567890", contact.PhoneNumber);
        }

        [Fact]
        public async Task GetContact_ReturnsNotFound_WhenNotExists()
        {
            var result = await _controller.GetContact(99);
            var actionResult = Assert.IsType<ActionResult<Contact>>(result);
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task PostContact_AddsContact_ReturnsCreatedAtAction()
        {
            var newContact = new Contact
            {
                Id = 3,
                Subject = "Alice Test",
                Email = "alice@example.com",
                Message = "Testing post",
                FullName = "Alice Wonderland",
                InquiryType = "Feedback",
                PhoneNumber = "1112223333"
            };

            var result = await _controller.PostContact(newContact);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            var contact = Assert.IsType<Contact>(created.Value);
            Assert.Equal("Alice Test", contact.Subject);
            Assert.Equal("Alice Wonderland", contact.FullName);
            Assert.Equal("Feedback", contact.InquiryType);
            Assert.Equal("1112223333", contact.PhoneNumber);

            var allContacts = _context.Contact.ToList();
            Assert.Equal(3, allContacts.Count);
        }

        [Fact]
        public async Task PutContact_UpdatesContact_ReturnsNoContent()
        {
            var existing = _context.Contact.First();
            existing.Message = "Updated message";
            existing.FullName = "Updated Name";
            existing.InquiryType = "Updated Inquiry";
            existing.PhoneNumber = "9998887777";

            var result = await _controller.PutContact(existing.Id, existing);
            Assert.IsType<NoContentResult>(result);

            var updated = _context.Contact.Find(existing.Id);
            Assert.Equal("Updated message", updated.Message);
            Assert.Equal("Updated Name", updated.FullName);
            Assert.Equal("Updated Inquiry", updated.InquiryType);
            Assert.Equal("9998887777", updated.PhoneNumber);
        }

        [Fact]
        public async Task PutContact_ReturnsBadRequest_WhenIdMismatch()
        {
            var existing = _context.Contact.First();
            var result = await _controller.PutContact(99, existing);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task DeleteContact_RemovesContact_ReturnsNoContent()
        {
            var existing = _context.Contact.First();
            var result = await _controller.DeleteContact(existing.Id);
            Assert.IsType<NoContentResult>(result);

            var contact = await _context.Contact.FindAsync(existing.Id);
            Assert.Null(contact);
        }

        [Fact]
        public async Task DeleteContact_ReturnsNotFound_WhenIdDoesNotExist()
        {
            var result = await _controller.DeleteContact(999);
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
