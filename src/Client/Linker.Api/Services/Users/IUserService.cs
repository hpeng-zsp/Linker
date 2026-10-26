using Linker.Api.Contracts.Users;

namespace Linker.Api.Services.Users;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        long id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default);
}