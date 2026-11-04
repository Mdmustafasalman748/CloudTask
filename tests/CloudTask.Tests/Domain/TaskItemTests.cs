using CloudTask.Domain.Entities;
using CloudTask.Domain.Enums;
using System;

namespace CloudTask.Tests.Domain;

public class TaskItemTests
{
    [Fact]
    public void NewTask_StartsAsTodo()
    {
        var task = new TaskItem { Title = "Write docs" };

        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void Start_FromTodo_MovesToInProgress()
    {
        var task = new TaskItem { Title = "Write docs" };

        task.Start();

        Assert.Equal(TaskItemStatus.InProgress, task.Status);
    }

    [Fact]
    public void Complete_SetsStatusAndCompletedAt()
    {
        var task = new TaskItem { Title = "Write docs" };

        task.Complete();

        Assert.Equal(TaskItemStatus.Done, task.Status);
        Assert.NotNull(task.CompletedAt);
    }

    [Fact]
    public void Complete_WhenAlreadyDone_Throws()
    {
        var task = new TaskItem { Title = "Write docs" };
        task.Complete();

        Assert.Throws<InvalidOperationException>(() => task.Complete());
    }

    [Fact]
    public void AssignTo_SetsAssignee()
    {
        var task = new TaskItem { Title = "Write docs" };
        var user = new User { Id = 5, Email = "a@b.com" };

        task.AssignTo(user);

        Assert.Equal(5, task.AssigneeId);
        Assert.Same(user, task.Assignee);
    }

    [Fact]
    public void AssignTo_CompletedTask_Throws()
    {
        var task = new TaskItem { Title = "Write docs" };
        task.Complete();

        Assert.Throws<InvalidOperationException>(() => task.AssignTo(new User { Id = 1 }));
    }
}