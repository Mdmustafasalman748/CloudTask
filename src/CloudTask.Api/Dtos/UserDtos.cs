using System.ComponentModel.DataAnnotations;

namespace CloudTask.Api.Dtos;

public record UserResponse(int Id, string Email, string DisplayName, DateTime CreatedAt);

public class CreateUserRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; set; } = string.Empty;
}