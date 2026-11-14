using System.ComponentModel.DataAnnotations;
using CloudTask.Domain.Enums;

namespace CloudTask.Api.Dtos;

public class TaskQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public TaskItemStatus? Status { get; set; }

    // id | title | createdAt
    public string SortBy { get; set; } = "id";

    public bool Descending { get; set; }
}