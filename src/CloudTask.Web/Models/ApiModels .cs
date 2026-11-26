namespace CloudTask.Web.Models;

public record UserDto(int Id, string Email, string DisplayName);

public record LoginResultDto(string Token, DateTime ExpiresAt, UserDto User);

public record ProjectDto(int Id, string Name, string? Description, int TaskCount);

public record PagedDto<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record TaskDto(
    int Id, string Title, string Status, string Priority, int ProjectId, int? AssigneeId);

public record ProjectDetailDto(int Id, string Name, string? Description, int OwnerId, int TaskCount);