using FileManager.Application.DTOs;
using FileManager.Application.Interfaces;
using FileManager.Domain.Enums;
using FileManager.Domain.Entities;
using FileManager.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Admin;

[Authorize]
public class LogsModel : PageModel
{
    private readonly IAuditService _auditService;
    private readonly IUserService _userService;

    public LogsModel(IAuditService auditService, IUserService userService)
    {
        _auditService = auditService;
        _userService = userService;
    }

    public List<AuditLog> Logs { get; set; } = new();
    public List<UserDto> Users { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? UserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public AuditAction? Action { get; set; }

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        Users = await _userService.GetAllUsersAsync();
        Logs = (await _auditService.GetLogsAsync(From, To, UserId, Action)).ToList();
    }
}
