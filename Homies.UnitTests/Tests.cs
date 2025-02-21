using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using Homies.Controllers;
using Homies.Data.Data.Models;
using Homies.Models.Event;
using Homies.Data.Data;
using Type = Homies.Data.Data.Models.Type;

namespace Homies.Tests.Controllers
{
    public class EventControllerUnitTests
    {
        private HomiesDbContext _context;
        private EventController _controller;
        private Mock<ClaimsPrincipal> _mockUser;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<HomiesDbContext>()
                .UseInMemoryDatabase(databaseName: "HomiesUnitTestDb")
                .Options;

            _context = new HomiesDbContext(options);

            // Seed data
            SeedDatabase();

            _controller = new EventController(_context);

            // Mocking user claims
            _mockUser = new Mock<ClaimsPrincipal>();
            _mockUser.Setup(u => u.FindFirst(It.IsAny<string>())).Returns(new Claim(ClaimTypes.NameIdentifier, "test-user"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = _mockUser.Object }
            };
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task Add_GetMethod_ReturnsViewWithModel()
        {
            // Act
            var result = await _controller.Add();

            // Assert
            var viewResult = result as ViewResult;
            Assert.IsInstanceOf<ViewResult>(result);
            var model = viewResult?.Model as EventFormModel;
            Assert.IsNotNull(model);
            Assert.IsNotNull(model.Types);
            Assert.AreEqual(1, model.Types.Count()); // Checking if types are loaded
        }

        [Test]
        public async Task Add_PostMethodWithValidModel_ReturnsRedirect()
        {
            // Arrange
            var eventModel = new EventFormModel
            {
                Name = "New Event",
                Description = "New Description",
                Start = DateTime.Now.AddHours(1),
                End = DateTime.Now.AddHours(2),
                TypeId = 1
            };

            // Act
            var result = await _controller.Add(eventModel);

            // Assert
            Assert.IsInstanceOf<RedirectToActionResult>(result);
            Assert.AreEqual(3, _context.Events.Count()); // Verifying that an event was added
        }

        [Test]
        public async Task Add_PostMethodWithInvalidType_ReturnsViewWithError()
        {
            // Arrange
            var eventModel = new EventFormModel
            {
                Name = "New Event",
                Description = "New Description",
                Start = DateTime.Now.AddHours(1),
                End = DateTime.Now.AddHours(2),
                TypeId = 99 // Invalid TypeId
            };

            // Act
            var result = await _controller.Add(eventModel);

            // Assert
            Assert.IsInstanceOf<ViewResult>(result);
            Assert.IsFalse(_controller.ModelState.IsValid);
            Assert.AreEqual(2, _context.Events.Count()); // No event should be added
        }

        [Test]
        public async Task All_ReturnsAllEventsInView()
        {
            // Act
            var result = await _controller.All();

            // Assert
            var viewResult = result as ViewResult;
            Assert.IsInstanceOf<ViewResult>(result);
            var model = viewResult?.Model as List<EventViewShortModel>;
            Assert.IsNotNull(model);
            Assert.AreEqual(2, model.Count); // 2 events seeded in the database
        }

        [Test]
        public async Task Join_ValidEvent_AddsUserToEvent()
        {
            // Act
            var result = await _controller.Join(1); // Valid event ID

            // Assert
            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var participant = _context.EventsParticipants.FirstOrDefault(ep => ep.EventId == 1 && ep.HelperId == "test-user");
            Assert.IsNotNull(participant);
        }

        [Test]
        public async Task Join_InvalidEvent_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.Join(99); // Invalid event ID

            // Assert
            Assert.IsInstanceOf<BadRequestResult>(result);
        }

        [Test]
        public async Task Leave_ValidEvent_RemovesUserFromEvent()
        {
            // Arrange
            await _controller.Join(1); // Ensure user is joined first

            // Act
            var result = await _controller.Leave(1);

            // Assert
            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var participant = _context.EventsParticipants.FirstOrDefault(ep => ep.EventId == 1 && ep.HelperId == "test-user");
            Assert.IsNull(participant); // User should be removed
        }

        [Test]
        public async Task Edit_PostMethodWithValidModel_UpdatesEvent()
        {
            // Arrange
            var model = new EventFormModel
            {
                Name = "Updated Event",
                Description = "Updated Description",
                Start = DateTime.Now.AddHours(3),
                End = DateTime.Now.AddHours(4),
                TypeId = 1 // Valid Type
            };

            // Act
            var result = await _controller.Edit(1, model);

            // Assert
            Assert.IsInstanceOf<RedirectToActionResult>(result);
            var updatedEvent = await _context.Events.FindAsync(1);
            Assert.AreEqual("Updated Event", updatedEvent.Name);
            Assert.AreEqual("Updated Description", updatedEvent.Description);
        }

        // Helper method to seed data
        private void SeedDatabase()
        {
            var organiser = new IdentityUser { Id = "test-user", UserName = "TestUser" };
            _context.Users.Add(organiser);

            var type = new Type { Id = 1, Name = "Type 1" };
            _context.Types.Add(type);

            var event1 = new Event
            {
                Id = 1,
                Name = "Event 1",
                Description = "Description 1",
                OrganiserId = "test-user",
                Organiser = organiser,
                CreatedOn = DateTime.Now,
                Start = DateTime.Now.AddHours(1),
                End = DateTime.Now.AddHours(2),
                TypeId = 1,
                Type = type
            };

            var event2 = new Event
            {
                Id = 2,
                Name = "Event 2",
                Description = "Description 2",
                OrganiserId = "test-user",
                Organiser = organiser,
                CreatedOn = DateTime.Now,
                Start = DateTime.Now.AddHours(3),
                End = DateTime.Now.AddHours(4),
                TypeId = 1,
                Type = type
            };

            _context.Events.AddRange(event1, event2);
            _context.SaveChanges();
        }
    }
}
