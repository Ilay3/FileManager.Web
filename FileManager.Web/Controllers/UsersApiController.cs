using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Application.Services;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/users")]
public class UsersApiController : ControllerBase
{
    private readonly IUserService _userDtoService;
    private readonly UserService _userService;

    public UsersApiController(UserService userService, IUserService userDtoService)
    {
        _userService = userService;
        _userDtoService = userDtoService;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll([FromQuery] Guid? ownerId)
    {
        var users = await _userDtoService.GetAllUsersAsync();
        if (ownerId.HasValue)
        {
            users = users.Where(u => u.Id != ownerId.Value).ToList();
        }
        return Ok(users);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateUserAsync(request.Email, request.FullName, request.Password, request.Department, request.IsAdmin);
        var dto = await _userDtoService.GetUserByIdAsync(user.Id);
        return dto == null ? Problem("Не удалось создать пользователя") : Ok(dto);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        await _userService.CreateUserAsync(request.Email, request.FullName, request.Password, request.Department, false);
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _userService.DeleteUserAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPut("{id}/admin")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> UpdateAdmin(Guid id, [FromBody] UpdateAdminRequest request)
    {
        var user = await _userService.SetAdminStatusAsync(id, request.IsAdmin);
        if (user == null) return NotFound();
        var dto = await _userDtoService.GetUserByIdAsync(id);
        return Ok(dto);
    }

    public record CreateUserRequest(string Email, string FullName, string Password, string? Department, bool IsAdmin);
    public record RegisterRequest(string Email, string FullName, string Password, string? Department);
    public record UpdateAdminRequest(bool IsAdmin);
}
