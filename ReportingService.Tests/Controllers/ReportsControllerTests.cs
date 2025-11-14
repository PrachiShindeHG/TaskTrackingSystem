using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ReportingService.Controllers;
using ReportingService.Models;
using ReportingService.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskService.Models;

namespace ReportingService.Tests.Controllers
{
    [TestClass]
    public class ReportsControllerTests
    {
        private Mock<ITaskRepository> _mockTaskRepo;
        private Mock<ILogger<ReportsController>> _mockLogger;
        private ReportsController _controller;

        [TestInitialize]
        public void Setup()
        {
            _mockTaskRepo = new Mock<ITaskRepository>();
            _mockLogger = new Mock<ILogger<ReportsController>>();
            _controller = new ReportsController(_mockTaskRepo.Object, _mockLogger.Object);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private void SetAuthHeader(string token)
        {
            _controller.Request.Headers["Authorization"] = $"Bearer {token}";
        }

        /// <summary>
        /// Test method to get task of user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TasksByUser_MissingToken_ReturnsUnauthorized()
        {
            _controller.Request.Headers.Remove("Authorization");
            var result = await _controller.TasksByUser();
            Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
            var unauthorized = result as UnauthorizedObjectResult;
            Assert.AreEqual("Token required", unauthorized?.Value);
        }

        /// <summary>
        /// Test method to get task of user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TasksByUser_ValidToken_ReturnsReport()
        {
            SetAuthHeader("valid_token");
            var tasks = new List<TaskItem>
            {
                new() { AssigneeId = "U1", Status = "Open" },
                new() { AssigneeId = "U1", Status = "Completed" },
                new() { AssigneeId = "U2", Status = "Open" }
            };
            _mockTaskRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

            var result = await _controller.TasksByUser();

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var report = okResult.Value as List<TasksByUserReport>;
            Assert.IsNotNull(report);
            Assert.AreEqual(2, report.Count);

            var u1 = report.FirstOrDefault(r => r.UserId == "U1");
            Assert.IsNotNull(u1);
            Assert.AreEqual(2, u1.TotalTasks);
            Assert.AreEqual(1, u1.Open);
            Assert.AreEqual(1, u1.Completed);
        }

        /// <summary>
        /// Test method to get task of user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TasksByUser_UnassignedUser_ShowsUnassigned()
        {
            SetAuthHeader("valid_token");
            var tasks = new List<TaskItem>
            {
                new() { AssigneeId = null, Status = "Open" }
            };
            _mockTaskRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

            var result = await _controller.TasksByUser();

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var report = okResult.Value as List<TasksByUserReport>;
            Assert.IsNotNull(report);
            Assert.AreEqual("Unassigned", report[0].UserId);
        }

        /// <summary>
        /// Test method to get task of user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TasksByStatus_NoToken_ReturnsUnauthorized()
        {
            _controller.Request.Headers.Remove("Authorization");
            var result = await _controller.TasksByStatus();
            Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
        }

        /// <summary>
        /// Test method to get task of user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TasksByStatus_WithToken_GroupsByStatus()
        {
            SetAuthHeader("valid_token");
            var tasks = new List<TaskItem>
            {
                new() { Status = "Open" },
                new() { Status = "Open" },
                new() { Status = "In Progress" },
                new() { Status = null }
            };
            _mockTaskRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

            var result = await _controller.TasksByStatus();

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var report = okResult.Value as List<TasksByStatusReport>;
            Assert.IsNotNull(report);
            Assert.AreEqual(3, report.Count);

            var open = report.FirstOrDefault(r => r.Status == "Open");
            Assert.IsNotNull(open);
            Assert.AreEqual(2, open.Count);
        }

        /// <summary>
        /// Test method to get SLA report of user
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SLAReport_NoToken_ReturnsUnauthorized()
        {
            _controller.Request.Headers.Remove("Authorization");
            var result = await _controller.SLAReport();
            Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
        }

        /// <summary>
        /// Test method to create SLA report 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SLAReport_OnlyOverdueNonCompletedTasks()
        {
            SetAuthHeader("valid_token");
            var now = DateTime.UtcNow;
            var tasks = new List<TaskItem>
            {
                new() { Id = "T1", Title = "Overdue", DueDate = now.AddDays(-3), Status = "Open" },
                new() { Id = "T2", Title = "Done", DueDate = now.AddDays(-1), Status = "Completed" },
                new() { Id = "T3", Title = "Future", DueDate = now.AddDays(5), Status = "Open" },
                new() { Id = "T4", Title = "NoDue", DueDate = null, Status = "Open" }
            };
            _mockTaskRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

            var result = await _controller.SLAReport();

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var breaches = okResult.Value as List<SLAReportItem>;
            Assert.IsNotNull(breaches);
            Assert.AreEqual(1, breaches.Count);

            var breach = breaches[0];
            Assert.AreEqual("T1", breach.TaskId);
            Assert.AreEqual("Overdue", breach.Title);
            Assert.AreEqual(3, breach.DaysOverdue);
        }

        /// <summary>
        /// Test method to create SLA report 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SLAReport_OrdersByDaysOverdueDescending()
        {
            SetAuthHeader("valid_token");
            var now = DateTime.UtcNow.Date;
            var tasks = new List<TaskItem>
            {
                new() { Id = "T1", DueDate = now.AddDays(-10), Status = "Open" },
                new() { Id = "T2", DueDate = now.AddDays(-2), Status = "Open" },
                new() { Id = "T3", DueDate = now.AddDays(-5), Status = "Open" }
            };
            _mockTaskRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

            var result = await _controller.SLAReport();

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var breaches = okResult.Value as List<SLAReportItem>;
            Assert.IsNotNull(breaches);
            Assert.AreEqual(3, breaches.Count);

            Assert.AreEqual("T1", breaches[0].TaskId);
            Assert.AreEqual("T3", breaches[1].TaskId);
            Assert.AreEqual("T2", breaches[2].TaskId);
        }

        /// <summary>
        /// Test method to get SLA report 
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task SLAReport_UnassignedUser_ShowsUnassigned()
        {
            SetAuthHeader("valid_token");
            var tasks = new List<TaskItem>
            {
                new() { Id = "T1", AssigneeId = null, DueDate = DateTime.UtcNow.AddDays(-1), Status = "Open" }
            };
            _mockTaskRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(tasks);

            var result = await _controller.SLAReport();

            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            var breaches = okResult.Value as List<SLAReportItem>;
            Assert.AreEqual("Unassigned", breaches[0].AssigneeId);
        }

        /// <summary>
        /// Test method to get user tasks
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task RequireAuth_EmptyHeader_ReturnsUnauthorized()
        {
            _controller.Request.Headers.Remove("Authorization");
            var result = await _controller.TasksByUser();
            Assert.IsInstanceOfType(result, typeof(UnauthorizedObjectResult));
        }
    }
}
