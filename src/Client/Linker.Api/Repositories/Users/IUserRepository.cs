using Linker.Api.Contracts.Users;

namespace Linker.Api.Repositories.Users;

public interface IUserRepository
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<UserDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<UserDto> AddAsync(
        string name,
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(
        long id,
        string name,
        string email,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default);
}