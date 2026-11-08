using System.ComponentModel.DataAnnotations;

namespace CloudTask.Api.Dtos;

public record ProjectResponse(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    int OwnerId,
    int TaskCount);

public class CreateProjectRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int OwnerId { get; set; }
}

public class UpdateProjectRequest
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }
}