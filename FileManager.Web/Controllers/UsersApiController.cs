using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Application.Services;
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
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _userDtoService.GetAllUsersAsync();
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

    public record CreateUserRequest(string Email, string FullName, string Password, string? Department, bool IsAdmin);
    public record RegisterRequest(string Email, string FullName, string Password, string? Department);
}
