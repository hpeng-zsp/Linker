using System.ComponentModel.DataAnnotations;

namespace Linker.Api.Contracts.Users;

public sealed class CreateUserRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}