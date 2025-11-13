using Microsoft.AspNetCore.Mvc;
using TaskService.Models;

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

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] string? status,
        [FromQuery] string? assigneeId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var tasks = await _repo.FilterAsync(status, assigneeId, from, to);
        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var task = await _repo.GetByIdAsync(id);
        if (task == null) return NotFound();
        return Ok(task);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TaskItem task)
    {
        if (task == null) return BadRequest();
        var created = await _repo.CreateAsync(task);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] TaskItem updatedTask)
    {
        var userId = GetUserIdFromToken();
        if (userId == null) return Unauthorized("Token required");

        var existing = await _repo.GetByIdAsync(id);
        if (existing == null) return NotFound();

        // Log status change
        if (existing.Status != updatedTask.Status)
        {
            existing.ActivityLogs.Add(new ActivityLog
            {
                ChangedBy = userId,
                ChangeDescription = $"Status changed from {existing.Status} to {updatedTask.Status}"
            });
        }

        existing.Title = updatedTask.Title;
        existing.Description = updatedTask.Description;
        existing.Priority = updatedTask.Priority;
        existing.Status = updatedTask.Status;
        existing.AssigneeId = updatedTask.AssigneeId;
        existing.DueDate = updatedTask.DueDate;
        existing.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(id, existing);
        return NoContent();
    }

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

            // Optional: Only allow Admin or the assignee to delete
            // Uncomment next line if you want role restriction
            // var role = Request.Headers["Authorization"].ToString().Replace("Bearer ", "").Split('_')[1];
            // if (role != "Admin" && existingTask.AssigneeId != userId)
            //     return Forbid("You can only delete your own tasks or as Admin");

            // Log deletion activity
            existingTask.ActivityLogs.Add(new ActivityLog
            {
                ChangedBy = userId,
                ChangeDescription = "Task deleted"
            });

            // Save log before actual delete (optional — or just delete directly)
            await _repo.UpdateAsync(id, existingTask);

            // Now delete the task
            await _repo.DeleteAsync(id); // You need to add DeleteAsync in repository (see below)

            return NoContent(); // 204 - successful deletion
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting task {TaskId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}