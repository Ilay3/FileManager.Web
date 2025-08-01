using FileManager.Application.Services;
using FileManager.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace FileManager.Web.Pages.Files;

[Authorize]
public class IndexModel : PageModel
{
    private readonly FilesService _filesService;
    private readonly UserService _userService;

    public IndexModel(FilesService filesService, UserService userService)
    {
        _filesService = filesService;
        _userService = userService;
    }

    public IEnumerable<Domain.Entities.Files> Files { get; set; } = new List<Domain.Entities.Files>();

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            Files = await _filesService.GetUserFilesAsync(userId);

            // Обновляем последнюю активность пользователя
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _userService.UpdateLastActivityAsync(userId, ipAddress);
        }
    }
}