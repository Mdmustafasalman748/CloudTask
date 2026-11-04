using CloudTask.Domain.Enums;
using System;

namespace CloudTask.Domain.Entities;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskItemStatus Status { get; private set; } = TaskItemStatus.Todo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }

    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? AssigneeId { get; private set; }
    public User? Assignee { get; private set; }

    public void AssignTo(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (Status == TaskItemStatus.Done)
            throw new InvalidOperationException("Cannot assign a completed task.");

        Assignee = user;
        AssigneeId = user.Id;
    }

    public void Start()
    {
        if (Status != TaskItemStatus.Todo)
            throw new InvalidOperationException("Only tasks in Todo can be started.");

        Status = TaskItemStatus.InProgress;
    }

    public void Complete()
    {
        if (Status == TaskItemStatus.Done)
            throw new InvalidOperationException("Task is already completed.");

        Status = TaskItemStatus.Done;
        CompletedAt = DateTime.UtcNow;
    }
}