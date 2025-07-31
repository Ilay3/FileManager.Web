using FileManager.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Admin;

[Authorize]
public class IndexModel : PageModel
{
    private readonly StatisticsService _statisticsService;

    public IndexModel(StatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public int UsersCount { get; set; }
    public int FilesCount { get; set; }
    public int FoldersCount { get; set; }
    public long TotalSize { get; set; }

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        UsersCount = await _statisticsService.GetUsersCountAsync();
        FilesCount = await _statisticsService.GetFilesCountAsync();
        FoldersCount = await _statisticsService.GetFoldersCountAsync();
        TotalSize = await _statisticsService.GetTotalFileSizeAsync();
    }
}
