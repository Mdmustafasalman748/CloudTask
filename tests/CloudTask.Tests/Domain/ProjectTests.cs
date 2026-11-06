using CloudTask.Domain.Entities;
using CloudTask.Domain.Enums;
using System;

namespace CloudTask.Tests.Domain;

public class ProjectTests
{
    [Fact]
    public void AddTask_AddsTaskToProject()
    {
        var project = new Project { Id = 1, Name = "Demo" };

        var task = project.AddTask("  Design schema  ", TaskPriority.High);

        Assert.Single(project.Tasks);
        Assert.Equal("Design schema", task.Title);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(1, task.ProjectId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTask_WithEmptyTitle_Throws(string title)
    {
        var project = new Project { Id = 1, Name = "Demo" };

        Assert.Throws<ArgumentException>(() => project.AddTask(title));
    }
}