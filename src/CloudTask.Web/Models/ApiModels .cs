namespace CloudTask.Web.Models;

public record UserDto(int Id, string Email, string DisplayName);

public record LoginResultDto(string Token, DateTime ExpiresAt, UserDto User);

public record ProjectDto(int Id, string Name, string? Description, int TaskCount);

public record PagedDto<T>(List<T> Items, int TotalCount);