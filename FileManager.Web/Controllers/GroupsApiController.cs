using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Entities;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/groups")]
public class GroupsApiController : ControllerBase
{
    private readonly IAppDbContext _context;

    public GroupsApiController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<GroupDto>>> Get()
    {
        var groups = await _context.Groups
            .Select(g => new GroupDto(g.Id, g.Name))
            .ToListAsync();
        return Ok(groups);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<GroupDetailsDto>> GetById(Guid id)
    {
        var group = await _context.Groups
            .Include(g => g.Users)
            .FirstOrDefaultAsync(g => g.Id == id);
        if (group == null)
            return NotFound();

        var dto = new GroupDetailsDto(
            group.Id,
            group.Name,
            group.Users.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                FullName = u.FullName,
                Department = u.Department,
                IsAdmin = u.IsAdmin
            }).ToList());
        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<GroupDto>> Create([FromBody] CreateGroupRequest request)
    {
        var group = new Group { Name = request.Name, Description = request.Description };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();
        return Ok(new GroupDto(group.Id, group.Name));
    }

    [HttpPost("{groupId}/users/{userId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AddUser(Guid groupId, Guid userId)
    {
        var group = await _context.Groups.Include(g => g.Users).FirstOrDefaultAsync(g => g.Id == groupId);
        var user = await _context.Users.FindAsync(userId);
        if (group == null || user == null)
            return NotFound();

        if (!group.Users.Any(u => u.Id == userId))
        {
            group.Users.Add(user);
            await _context.SaveChangesAsync();
        }
        return NoContent();
    }

    [HttpDelete("{groupId}/users/{userId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> RemoveUser(Guid groupId, Guid userId)
    {
        var group = await _context.Groups.Include(g => g.Users).FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null)
            return NotFound();

        var user = group.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
            return NotFound();

        group.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var group = await _context.Groups.FindAsync(id);
        if (group == null)
            return NotFound();

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public record CreateGroupRequest(string Name, string? Description);
}
