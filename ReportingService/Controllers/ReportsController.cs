using Microsoft.AspNetCore.Mvc;
using ReportingService.Models;
using ReportingService.Repositories;
using System.Threading.Tasks;
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
                    New =g.Count(t=> t.Status == "New"),
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

        /// <summary>
        /// Get full task details + counts for the currently logged-in user
        /// Returns counts + list of tasks with all details
        /// </summary>
        [HttpGet("my-tasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            var auth = RequireAuth();
            if (auth is not OkResult) return auth;

            // Extract userId from your custom token format: "userId_role"
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
            var userId = token.Split('_')[0]; // This matches your current format

            var allTasks = await _taskRepo.GetAllAsync();

            var myTasks = allTasks
                .Where(t => t.AssigneeId == userId)
                .Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Description,
                    t.Status,
                    t.Priority,
                    t.DueDate,
                    t.CreatedAt,
                    t.UpdatedAt,
                    t.AssigneeId,
                    t.IsOverdue,
                    ActivityLogs = t.ActivityLogs
                        .Select(log => new
                        {
                            log.ChangedBy,
                            log.ChangeDescription,
                            Timestamp = log.Timestamp
                        })
                        .Take(3)
                        .ToList()
                })
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            var counts = new
            {
                TotalTasks = myTasks.Count(),
                Open = myTasks.Count(t => t.Status == "Open"),
                InProgress = myTasks.Count(t => t.Status == "In Progress"),
                New = myTasks.Count(t => t.Status == "New"),
                Blocked = myTasks.Count(t => t.Status == "Blocked"),
                Completed = myTasks.Count(t => t.Status == "Completed")
            };

            return Ok(new
            {
                Counts = counts,
                Tasks = myTasks
            });
        }
    }

}