using Linker.Api.Contracts.Users;
using Linker.Api.Repositories.Users;
using Linker.Core.Attributes;

namespace Linker.Api.Services.Users;

[Service]
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(
        IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public Task<IReadOnlyList<UserDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return _userRepository.GetAllAsync(
            cancellationToken);
    }

    public Task<UserDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return _userRepository.GetByIdAsync(
            id,
            cancellationToken);
    }

    public Task<UserDto> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _userRepository.AddAsync(
            request.Name,
            request.Email,
            cancellationToken);
    }

    public Task<bool> UpdateAsync(
        long id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _userRepository.UpdateAsync(
            id,
            request.Name,
            request.Email,
            cancellationToken);
    }

    public Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        return _userRepository.DeleteAsync(
            id,
            cancellationToken);
    }
}