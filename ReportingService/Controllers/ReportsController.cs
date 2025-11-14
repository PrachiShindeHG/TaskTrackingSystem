using Microsoft.AspNetCore.Mvc;
using ReportingService.Models;
using ReportingService.Repositories;
using TaskService.Models;

namespace ReportingService.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly ITaskRepository _taskRepo;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(ITaskRepository taskRepo, ILogger<ReportsController> logger)
        {
            _taskRepo = taskRepo;
            _logger = logger;
        }

        /// <summary>
        /// Check token presence
        /// </summary>
        /// <returns>Return token</returns>
        private IActionResult RequireAuth()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
            return string.IsNullOrEmpty(token) ? Unauthorized("Token required") : Ok();
        }

        /// <summary>
        /// Tasks By User
        /// </summary>
        /// <returns>Returns users task</returns>
        [HttpGet("tasks-by-user")]
        public async Task<IActionResult> TasksByUser()
        {
            var auth = RequireAuth(); if (auth is not OkResult) return auth;

            var tasks = await _taskRepo.GetAllAsync();

            var report = tasks
                .GroupBy(t => t.AssigneeId ?? "Unassigned")
                .Select(g => new TasksByUserReport
                {
                    UserId = g.Key,
                    TotalTasks = g.Count(),
                    Open = g.Count(t => t.Status == "Open"),
                    InProgress = g.Count(t => t.Status == "In Progress"),
                    Blocked = g.Count(t => t.Status == "Blocked"),
                    Completed = g.Count(t => t.Status == "Completed")
                })
                .OrderByDescending(r => r.TotalTasks)
                .ToList();

            return Ok(report);
        }

        /// <summary>
        /// Tasks By Status
        /// </summary>
        /// <returns>Returns users task by status</returns>
        [HttpGet("tasks-by-status")]
        public async Task<IActionResult> TasksByStatus()
        {
            var auth = RequireAuth(); if (auth is not OkResult) return auth;

            var tasks = await _taskRepo.GetAllAsync();

            var report = tasks
                .GroupBy(t => t.Status)
                .Select(g => new TasksByStatusReport
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            return Ok(report);
        }

        /// <summary>
        /// SLA Breaches (Overdue Tasks)
        /// </summary>
        /// <returns>Returns SLA report</returns>
        [HttpGet("sla-breaches")]
        public async Task<IActionResult> SLAReport()
        {
            var auth = RequireAuth(); if (auth is not OkResult) return auth;

            var tasks = await _taskRepo.GetAllAsync();

            var breaches = tasks
                .Where(t => t.DueDate.HasValue &&
                            t.DueDate < DateTime.UtcNow &&
                            t.Status != "Completed")
                .Select(t => new SLAReportItem
                {
                    TaskId = t.Id,
                    Title = t.Title,
                    AssigneeId = t.AssigneeId ?? "Unassigned",
                    DueDate = t.DueDate.Value,
                    DaysOverdue = (DateTime.UtcNow.Date - t.DueDate.Value.Date).Days
                })
                .OrderByDescending(x => x.DaysOverdue)
                .ToList();

            return Ok(breaches);
        }
    }
}