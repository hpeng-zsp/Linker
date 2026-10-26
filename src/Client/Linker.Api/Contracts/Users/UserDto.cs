namespace Linker.Api.Contracts.Users;

public sealed class UserDto
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }
}