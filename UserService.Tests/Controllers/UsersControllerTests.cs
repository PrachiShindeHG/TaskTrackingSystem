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

        /// <summary>
        /// Test method to get user's list
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetAll_ReturnsOk()
        {
            var users = new List<User> { new User { Id = "1" } };
            _mockRepo.Setup(r => r.GetAllUserAsync()).ReturnsAsync(users);

            var result = await _controller.GetAllUsers();

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(users, okResult.Value);
        }

        /// <summary>
        /// Test method to get user's list negative flow
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetAll_RepositoryThrows_ReturnsInternalServerError()
        {
            _mockRepo.Setup(r => r.GetAllUserAsync()).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetAllUsers();

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }

        /// <summary>
        /// Test method to get user by Id
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetById_Found_ReturnsOk()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync(user);

            var result = await _controller.GetUserById("1");

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(user, okResult.Value);
        }

        /// <summary>
        /// Test method to get user by Id negative flow
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetById_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync((User)null);

            var result = await _controller.GetUserById("1");

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        /// <summary>
        /// Test method to get user by Id negative flow
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task GetById_ReturnsInternalServerError()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetUserById("1");

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }

        /// <summary>
        /// Test method to create user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Create_NullUser_ReturnsBadRequest()
        {
            var result = await _controller.CreateUser(null);

            var badRequest = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequest);
            Assert.AreEqual("Invalid user data", badRequest.Value);
        }

        /// <summary>
        /// Test method to create user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Create_ValidUser_ReturnsCreatedAtAction()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.CreateUserAsync(user)).ReturnsAsync(user);

            var result = await _controller.CreateUser(user);

            var createdResult = result as CreatedAtActionResult;
            Assert.IsNotNull(createdResult);
            Assert.AreEqual(user, createdResult.Value);
            Assert.AreEqual("GetById", createdResult.ActionName);
            Assert.AreEqual("1", createdResult.RouteValues["id"]);
        }

        /// <summary>
        /// Test method to create user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Create_ReturnsInternalServerError()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.CreateUserAsync(user)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.CreateUser(user);

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }

        /// <summary>
        /// Test method to update user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Update_NullUser_ReturnsBadRequest()
        {
            var result = await _controller.UpdateUser("1", null);

            var badRequest = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequest);
            Assert.AreEqual("Invalid user data", badRequest.Value);
        }

        /// <summary>
        /// Test method to update user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync((User)null);

            var user = new User { Id = "1" };
            var result = await _controller.UpdateUser("1", user);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        /// <summary>
        /// Test method to update user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Update_Valid_ReturnsNoContent()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync(user);

            var updatedUser = new User { Id = "1" };
            var result = await _controller.UpdateUser("1", updatedUser);

            _mockRepo.Verify(r => r.UpdateUserAsync("1", It.Is<User>(u => u.Id == "1")), Times.Once);
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        /// <summary>
        /// Test method to update user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Update_ReturnsInternalServerError()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync(user);
            _mockRepo.Setup(r => r.UpdateUserAsync("1", user)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.UpdateUser("1", user);

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }

        /// <summary>
        /// Test method to delete user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync((User)null);

            var result = await _controller.DeleteUser("1");

            var notFound = result as NotFoundObjectResult;
            Assert.IsNotNull(notFound);
            Assert.IsTrue(notFound.Value.ToString().Contains("User with id 1 not found"));
        }

        /// <summary>
        /// Test method to delete user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Delete_NoAuthHeader_ReturnsUnauthorized()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync(user);

            var result = await _controller.DeleteUser("1");

            var unauthorized = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorized);
            Assert.AreEqual("Authorization header is missing", unauthorized.Value);
        }

        /// <summary>
        /// Test method to delete user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Delete_InvalidTokenFormat_ReturnsUnauthorized()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync(user);
            SetAuthHeader("BadToken");

            var result = await _controller.DeleteUser("1");

            var unauthorized = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorized);
            Assert.AreEqual("Invalid token format", unauthorized.Value);
        }

        /// <summary>
        /// Test method to delete user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Delete_NotAdmin_ReturnsForbid()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync(user);
            SetAuthHeader("Bearer user1_User");

            var result = await _controller.DeleteUser("1");

            var forbid = result as ForbidResult;
            Assert.IsNotNull(forbid);
        }

        /// <summary>
        /// Test method to delete user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Delete_Admin_DeletesUser_ReturnsNoContent()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync(user);
            SetAuthHeader("Bearer user1_Admin");

            var result = await _controller.DeleteUser("1");

            _mockRepo.Verify(r => r.DeleteAsync("1"), Times.Once);
            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        /// <summary>
        /// Test method to delete user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Delete_ReturnsInternalServerError()
        {
            var user = new User { Id = "1" };
            _mockRepo.Setup(r => r.GetUserByIdAsync("1")).ReturnsAsync(user);
            SetAuthHeader("Bearer user1_Admin");
            _mockRepo.Setup(r => r.DeleteAsync("1")).ThrowsAsync(new Exception("fail"));

            var result = await _controller.DeleteUser("1");

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }
    }
}

