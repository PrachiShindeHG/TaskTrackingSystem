using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserService.Models;
using UserService.Controllers;

namespace UserService.Tests.Controllers
{
    [TestClass]
    public class UsersControllerTests
    {
        private Mock<IUserRepository> _mockRepo;
        private Mock<ILogger<UsersController>> _mockLogger;
        private UsersController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockRepo = new Mock<IUserRepository>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _controller = new UsersController(_mockRepo.Object, _mockLogger.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private void SetAuthHeader(string token)
        {
            _controller.Request.Headers["Authorization"] = token;
        }

        [TestMethod]
        public async Task GetAll_ReturnsOk()
        {
            var users = new List<User> { new User { Id = "1" } };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(users);

            var result = await _controller.GetAll();

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(users, okResult.Value);
        }

        [TestMethod]
        public async Task GetAll_RepositoryThrows_ReturnsInternalServerError()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetAll();

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }

        [TestMethod]
        public async Task GetById_Found_ReturnsOk()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);

            var result = await _controller.GetById("1");

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(user, okResult.Value);
        }

        [TestMethod]
        public async Task GetById_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync((User)null);

            var result = await _controller.GetById("1");

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task GetById_RepositoryThrows_ReturnsInternalServerError()
        {
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetById("1");

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }

        [TestMethod]
        public async Task Create_NullUser_ReturnsBadRequest()
        {
            var result = await _controller.Create(null);

            var badRequest = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequest);
            Assert.AreEqual("Invalid user data", badRequest.Value);
        }

        [TestMethod]
        public async Task Create_ValidUser_ReturnsCreatedAtAction()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.CreateAsync(user)).ReturnsAsync(user);

            var result = await _controller.Create(user);

            var createdResult = result as CreatedAtActionResult;
            Assert.IsNotNull(createdResult);
            Assert.AreEqual(user, createdResult.Value);
            Assert.AreEqual("GetById", createdResult.ActionName);
            Assert.AreEqual("1", createdResult.RouteValues["id"]);
        }

        [TestMethod]
        public async Task Create_RepositoryThrows_ReturnsInternalServerError()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.CreateAsync(user)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.Create(user);

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }

        [TestMethod]
        public async Task Update_NullUser_ReturnsBadRequest()
        {
            var result = await _controller.Update("1", null);

            var badRequest = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequest);
            Assert.AreEqual("Invalid user data", badRequest.Value);
        }

        [TestMethod]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync((User)null);

            var user = new User { Id = "1" };
            var result = await _controller.Update("1", user);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Update_Valid_ReturnsNoContent()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);

            var updatedUser = new User { Id = "1" };
            var result = await _controller.Update("1", updatedUser);

            _mockRepo.Verify(r => r.UpdateAsync("1", It.Is<User>(u => u.Id == "1")), Times.Once);
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task Update_RepositoryThrows_ReturnsInternalServerError()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateAsync("1", user)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.Update("1", user);

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }

        [TestMethod]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync((User)null);

            var result = await _controller.Delete("1");

            var notFound = result as NotFoundObjectResult;
            Assert.IsNotNull(notFound);
            Assert.IsTrue(notFound.Value.ToString().Contains("User with id 1 not found"));
        }

        [TestMethod]
        public async Task Delete_NoAuthHeader_ReturnsUnauthorized()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);

            var result = await _controller.Delete("1");

            var unauthorized = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorized);
            Assert.AreEqual("Authorization header is missing", unauthorized.Value);
        }

        [TestMethod]
        public async Task Delete_InvalidTokenFormat_ReturnsUnauthorized()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);
            SetAuthHeader("BadToken");

            var result = await _controller.Delete("1");

            var unauthorized = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorized);
            Assert.AreEqual("Invalid token format", unauthorized.Value);
        }

        [TestMethod]
        public async Task Delete_NotAdmin_ReturnsForbid()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);
            SetAuthHeader("Bearer user1_User");

            var result = await _controller.Delete("1");

            var forbid = result as ForbidResult;
            Assert.IsNotNull(forbid);
        }

        [TestMethod]
        public async Task Delete_Admin_DeletesUser_ReturnsNoContent()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);
            SetAuthHeader("Bearer user1_Admin");

            var result = await _controller.Delete("1");

            _mockRepo.Verify(r => r.DeleteAsync("1"), Times.Once);
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task Delete_RepositoryThrows_ReturnsInternalServerError()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);
            SetAuthHeader("Bearer user1_Admin");
            _mockRepo.Setup(r => r.DeleteAsync("1")).ThrowsAsync(new Exception("fail"));

            var result = await _controller.Delete("1");

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }
    }
}

