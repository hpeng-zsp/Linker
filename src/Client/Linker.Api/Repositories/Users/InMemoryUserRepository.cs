using System.Collections.Concurrent;
using Linker.Api.Contracts.Users;
using Linker.Core.Attributes;

namespace Linker.Api.Repositories.Users;

[Repository]
public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<long, UserDto>
        _users = new();

    private long _idSequence;

    public InMemoryUserRepository()
    {
        var user1 = CreateSeedUser(
            "张三",
            "zhangsan@example.com");

        var user2 = CreateSeedUser(
            "李四",
            "lisi@example.com");

        _users.TryAdd(user1.Id, user1);
        _users.TryAdd(user2.Id, user2);
    }

    public Task<IReadOnlyList<UserDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<UserDto> result = _users.Values
            .OrderBy(x => x.Id)
            .ToArray();

        return Task.FromResult(result);
    }

    public Task<UserDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _users.TryGetValue(id, out var user);

        return Task.FromResult(user);
    }

    public Task<UserDto> AddAsync(
        string name,
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var id = Interlocked.Increment(
            ref _idSequence);

        var user = new UserDto
        {
            Id = id,
            Name = name,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow
        };

        _users[id] = user;

        return Task.FromResult(user);
    }

    public Task<bool> UpdateAsync(
        long id,
        string name,
        string email,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_users.ContainsKey(id))
        {
            return Task.FromResult(false);
        }

        var updatedUser = new UserDto
        {
            Id = id,
            Name = name,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow
        };

        _users[id] = updatedUser;

        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var deleted = _users.TryRemove(
            id,
            out _);

        return Task.FromResult(deleted);
    }

    private UserDto CreateSeedUser(
        string name,
        string email)
    {
        var id = Interlocked.Increment(
            ref _idSequence);

        return new UserDto
        {
            Id = id,
            Name = name,
            Email = email,
            CreatedAtUtc = DateTime.UtcNow
        };
    }
}