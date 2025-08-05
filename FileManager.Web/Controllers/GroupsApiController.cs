using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
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
}
