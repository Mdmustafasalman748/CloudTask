using System.ComponentModel.DataAnnotations;
using CloudTask.Domain.Entities;
using CloudTask.Domain.Enums;

namespace CloudTask.Api.Dtos;

public record TaskResponse(
    int Id,
    string Title,
    string? Description,
    string Status,
    string Priority,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    int ProjectId,
    int? AssigneeId)
{
    public static TaskResponse From(TaskItem t) => new(
        t.Id, t.Title, t.Description, t.Status.ToString(), t.Priority.ToString(),
        t.CreatedAt, t.CompletedAt, t.ProjectId, t.AssigneeId);
}

public class CreateTaskRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
}

public class AssignTaskRequest
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }
}