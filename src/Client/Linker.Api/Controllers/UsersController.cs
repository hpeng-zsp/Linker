using Linker.Api.Contracts.Users;
using Linker.Api.Services.Users;
using Microsoft.AspNetCore.Mvc;

namespace Linker.Api.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(
        IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 获取所有用户。
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<UserDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await _userService.GetAllAsync(
            cancellationToken);

        return Ok(users);
    }

    /// <summary>
    /// 根据 ID 获取用户。
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(
        typeof(UserDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetByIdAsync(
            id,
            cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// 创建用户。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(
        typeof(UserDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userService.CreateAsync(
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new
            {
                id = user.Id
            },
            user);
    }

    /// <summary>
    /// 更新用户。
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _userService.UpdateAsync(
            id,
            request,
            cancellationToken);

        if (!updated)
        {
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// 删除用户。
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        long id,
        CancellationToken cancellationToken)
    {
        var deleted = await _userService.DeleteAsync(
            id,
            cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}