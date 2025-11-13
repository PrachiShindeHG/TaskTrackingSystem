using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskService.Models;

namespace TaskService.Tests.Controllers
{
    [TestClass]
    public class TasksControllerTests
    {
        private Mock<ITaskRepository> _mockRepo;
        private Mock<ILogger<TasksController>> _mockLogger;
        private TasksController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockRepo = new Mock<ITaskRepository>();
            _mockLogger = new Mock<ILogger<TasksController>>();
            _controller = new TasksController(_mockRepo.Object, _mockLogger.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private void SetAuthHeader(string token)
        {
            _controller.Request.Headers["Authorization"] = $"Bearer {token}";
        }

        [TestMethod]
        public async Task Get_ReturnsFilteredTasks()
        {
            var tasks = new List<TaskItem> { new TaskItem { Id = "1" } };
            _mockRepo.Setup(r => r.FilterAsync("Open", "U1", null, null)).ReturnsAsync(tasks);

            var result = await _controller.Get("Open", "U1", null, null);

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(tasks, okResult.Value);
        }

        [TestMethod]
        public async Task GetById_Found_ReturnsOk()
        {
            var task = new TaskItem { Id = "1" };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(task);

            var result = await _controller.GetById("1");

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(task, okResult.Value);
        }

        [TestMethod]
        public async Task GetById_NotFound_ReturnsNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync((TaskItem)null);

            var result = await _controller.GetById("1");

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Create_NullTask_ReturnsBadRequest()
        {
            var result = await _controller.Create(null);

            Assert.IsInstanceOfType(result, typeof(BadRequestResult));
        }

        [TestMethod]
        public async Task Create_ValidTask_ReturnsCreatedAtAction()
        {
            var task = new TaskItem { Id = "1" };
            _mockRepo.Setup(r => r.CreateAsync(task)).ReturnsAsync(task);

            var result = await _controller.Create(task);

            var createdResult = result as CreatedAtActionResult;
            Assert.IsNotNull(createdResult);
            Assert.AreEqual(task, createdResult.Value);
            Assert.AreEqual("GetById", createdResult.ActionName);
            Assert.AreEqual("1", createdResult.RouteValues["id"]);
        }

        [TestMethod]
        public async Task Update_NoToken_ReturnsUnauthorized()
        {
            _controller.Request.Headers.Remove("Authorization");
            var updatedTask = new TaskItem { Id = "1" };

            var result = await _controller.Update("1", updatedTask);

            var unauthorized = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorized);
            Assert.AreEqual("Token required", unauthorized.Value);
        }

        [TestMethod]
        public async Task Update_NotFound_ReturnsNotFound()
        {
            SetAuthHeader("U1_role");
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync((TaskItem)null);

            var updatedTask = new TaskItem { Id = "1" };
            var result = await _controller.Update("1", updatedTask);

            Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Update_StatusChanged_LogsActivity_ReturnsNoContent()
        {
            SetAuthHeader("U1_role");
            var existing = new TaskItem
            {
                Id = "1",
                Status = "Open",
                ActivityLogs = new List<ActivityLog>()
            };
            var updatedTask = new TaskItem
            {
                Id = "1",
                Status = "Completed",
                Title = "T",
                Description = "D",
                Priority = "High",
                AssigneeId = "U1",
                DueDate = DateTime.UtcNow
            };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);

            var result = await _controller.Update("1", updatedTask);

            _mockRepo.Verify(r => r.UpdateAsync("1", It.Is<TaskItem>(t =>
                t.ActivityLogs.Count == 1 &&
                t.ActivityLogs[0].ChangedBy == "U1" &&
                t.ActivityLogs[0].ChangeDescription.Contains("Status changed")
            )), Times.Once);

            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task Update_StatusUnchanged_NoActivityLog_ReturnsNoContent()
        {
            SetAuthHeader("U1_role");
            var existing = new TaskItem
            {
                Id = "1",
                Status = "Open",
                ActivityLogs = new List<ActivityLog>()
            };
            var updatedTask = new TaskItem
            {
                Id = "1",
                Status = "Open",
                Title = "T",
                Description = "D",
                Priority = "High",
                AssigneeId = "U1",
                DueDate = DateTime.UtcNow
            };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);

            var result = await _controller.Update("1", updatedTask);

            _mockRepo.Verify(r => r.UpdateAsync("1", It.Is<TaskItem>(t =>
                t.ActivityLogs.Count == 0
            )), Times.Once);

            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task Delete_NoToken_ReturnsUnauthorized()
        {
            _controller.Request.Headers.Remove("Authorization");
            var result = await _controller.Delete("1");

            var unauthorized = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorized);
            Assert.AreEqual("Valid token is required", unauthorized.Value);
        }

        [TestMethod]
        public async Task Delete_NotFound_ReturnsNotFound()
        {
            SetAuthHeader("U1_role");
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync((TaskItem)null);

            var result = await _controller.Delete("1");

            var notFound = result as NotFoundObjectResult;
            Assert.IsNotNull(notFound);
            Assert.IsTrue(notFound.Value.ToString().Contains("Task with id 1 not found"));
        }

        [TestMethod]
        public async Task Delete_Valid_LogsActivity_DeletesTask_ReturnsNoContent()
        {
            SetAuthHeader("U1_role");
            var existing = new TaskItem
            {
                Id = "1",
                AssigneeId = "U1",
                ActivityLogs = new List<ActivityLog>()
            };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);

            var result = await _controller.Delete("1");

            _mockRepo.Verify(r => r.UpdateAsync("1", It.Is<TaskItem>(t =>
                t.ActivityLogs.Count == 1 &&
                t.ActivityLogs[0].ChangedBy == "U1" &&
                t.ActivityLogs[0].ChangeDescription == "Task deleted"
            )), Times.Once);

            _mockRepo.Verify(r => r.DeleteAsync("1"), Times.Once);

            Assert.IsInstanceOfType(result, typeof(NoContentResult));
        }

        [TestMethod]
        public async Task Delete_Exception_ReturnsInternalServerError()
        {
            SetAuthHeader("U1_role");
            var existing = new TaskItem
            {
                Id = "1",
                AssigneeId = "U1",
                ActivityLogs = new List<ActivityLog>()
            };
            _mockRepo.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(existing);
            _mockRepo.Setup(r => r.UpdateAsync("1", It.IsAny<TaskItem>())).ThrowsAsync(new Exception("fail"));

            var result = await _controller.Delete("1");

            var statusResult = result as ObjectResult;
            Assert.IsNotNull(statusResult);
            Assert.AreEqual(500, statusResult.StatusCode);
            Assert.AreEqual("Internal server error", statusResult.Value);
        }
    }
}
