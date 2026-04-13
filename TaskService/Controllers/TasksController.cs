using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using TaskService.Models;

namespace TaskService.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _repo;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskRepository repo, ILogger<TasksController> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        private string? GetUserIdFromToken()
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            return string.IsNullOrEmpty(token) ? null : token.Split('_')[0];
        }

        private async Task<string?> GetUsernameFromTokenAsync()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrWhiteSpace(authHeader)) return null;

            var token = authHeader.Replace("Bearer ", "").Trim();
            if (string.IsNullOrWhiteSpace(token)) return null;

            var parts = token.Split('_', 2);
            var userId = parts.Length > 0 ? parts[0] : null;
            if (string.IsNullOrWhiteSpace(userId)) return null;

            try
            {
                var username = await _repo.GetUsernameById(userId);
                return string.IsNullOrWhiteSpace(username) ? userId : username;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve username for token userId={UserId}", userId);
                return userId;
            }
        }

        /// <summary>
        /// Method to get task using filters
        /// </summary>
        /// <param name="status"></param>
        /// <param name="assigneeId"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>Returns 200 OK</returns>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? status, [FromQuery] string? assigneeId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            // Use repository FilterAsync so query parameters are applied.
            // When all parameters are null/empty the repository returns all tasks.
            var tasks = await _repo.FilterAsync(status, assigneeId, from, to);

            // Batch username resolution to avoid N+1 queries.
            var userIds = tasks
                .SelectMany(t => new[] { t.AssigneeId, t.CreatedById })
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var usernameTasks = userIds.ToDictionary(uid => uid, uid => _repo.GetUsernameById(uid));
            await Task.WhenAll(usernameTasks.Values);

            var usernameMap = usernameTasks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Result ?? kvp.Key);

            var result = tasks.Select(task =>
            {
                usernameMap.TryGetValue(task.AssigneeId ?? string.Empty, out var assigneeUsername);
                usernameMap.TryGetValue(task.CreatedById ?? string.Empty, out var createdByUsername);

                return new
                {
                    task.Id,
                    task.Title,
                    task.Description,
                    task.Status,
                    task.Priority,
                    task.DueDate,
                    task.CreatedAt,
                    task.UpdatedAt,
                    AssigneeId = task.AssigneeId,
                    AssigneeUsername = string.IsNullOrWhiteSpace(assigneeUsername) ? null : assigneeUsername,
                    CreatedById = task.CreatedById,
                    CreatedByUsername = string.IsNullOrWhiteSpace(createdByUsername) ? null : createdByUsername
                };
            }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// Method to get tasks by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Returns 200 OK</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var task = await _repo.GetByIdAsync(id);
            if (task == null) return NotFound();
            return Ok(task);
        }

        /// <summary>
        /// Method to create task
        /// </summary>
        /// <param name="task"></param>
        /// <returns>Returns 201 Created</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskItem task)
        {
            if (task == null) return BadRequest();
            var created = await _repo.CreateAsync(task);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        /// <summary>
        /// Method to update tasks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updatedTask"></param>
        /// <returns>Returns 204</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] TaskItem updatedTask)
        {
            var userId = await GetUsernameFromTokenAsync();
            if (userId == null) return Unauthorized("Token required");

            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();
            if (existing.Status != updatedTask.Status)
            {
                existing.ActivityLogs.Add(new ActivityLog
                {
                    ChangedBy = userId,
                    ChangeDescription = $"Status changed from {existing.Status} to {updatedTask.Status}",
                    Timestamp = DateTime.UtcNow,
                    FromStatus = existing.Status,
                    ToStatus = updatedTask.Status
                });
            }

            //// Log status change if different
            //if (!string.Equals(existing.Status, updatedTask.Status, StringComparison.OrdinalIgnoreCase))
            //{
            //    existing.StatusChangeLogs.Add(new ActivityLog
            //    {
            //        ChangedBy = userId,  
            //        FromStatus = existing.Status,
            //        ToStatus = updatedTask.Status,
            //        Timestamp = DateTime.UtcNow
            //    });
            //}

            existing.Title = updatedTask.Title;
            existing.Description = updatedTask.Description;
            existing.Priority = updatedTask.Priority;
            existing.Status = updatedTask.Status;
            existing.AssigneeId = updatedTask.AssigneeId;
            existing.AssigneeName = updatedTask.AssigneeName;
            existing.DueDate = updatedTask.DueDate;
            existing.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(id, existing);
            return NoContent();
        }

        /// <summary>
        /// Method to delete the task
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Returns 204</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var userId = GetUserIdFromToken();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Valid token is required");

                var existingTask = await _repo.GetByIdAsync(id);
                if (existingTask == null)
                    return NotFound($"Task with id {id} not found");

                var authToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Trim();
                var tokenParts = authToken.Split('_');
                var role = tokenParts.Length >= 2 ? tokenParts[^1] : string.Empty;
                if (!role.Equals("Admin", System.StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid("Only Admin can delete the tasks");
                }

                // Log deletion activity
                existingTask.ActivityLogs.Add(new ActivityLog
                {
                    ChangedBy = userId,
                    ChangeDescription = "Task deleted"
                });

                // Save log before actual delete 
                await _repo.UpdateAsync(id, existingTask);

                // delete the task
                await _repo.DeleteAsync(id); 

                return NoContent(); // 204 - successful deletion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task {TaskId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}