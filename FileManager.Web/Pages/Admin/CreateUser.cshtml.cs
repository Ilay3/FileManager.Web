using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FileManager.Web.Pages.Admin;

[Authorize]
public class CreateUserModel : PageModel
{
    public void OnGet()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
        }
    }
}
