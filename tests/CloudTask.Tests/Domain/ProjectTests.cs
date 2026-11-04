using CloudTask.Domain.Enums;
using System.Collections.Generic;
using System;

namespace CloudTask.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int OwnerId { get; set; }
    public User? Owner { get; set; }

    public List<TaskItem> Tasks { get; set; } = new();

    public TaskItem AddTask(string title, TaskPriority priority = TaskPriority.Medium)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Task title is required.", nameof(title));

        var task = new TaskItem
        {
            Title = title.Trim(),
            Priority = priority,
            ProjectId = Id
        };

        Tasks.Add(task);
        return task;
    }
}