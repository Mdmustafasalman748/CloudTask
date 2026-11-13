using System.ComponentModel.DataAnnotations;

namespace CloudTask.Api.Dtos;

public class ProjectQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    // The server-side cap: clients can never ask for more than 100 rows
    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    [StringLength(100)]
    public string? Search { get; set; }

    // id | name | createdAt
    public string SortBy { get; set; } = "id";

    public bool Descending { get; set; }
}