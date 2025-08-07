using FileManager.Application.Interfaces;
using FileManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FileManager.Web.Pages.Admin.Settings;

[Authorize]
public class PermissionsModel : PageModel
{
    private readonly IGroupService _groupService;

    public PermissionsModel(IGroupService groupService)
    {
        _groupService = groupService;
    }

    [BindProperty]
    public List<GroupPermissionViewModel> Groups { get; set; } = new();

    public class GroupPermissionViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Read { get; set; }
        public bool Write { get; set; }
        public bool Delete { get; set; }
        public bool ManageAccess { get; set; }
        public bool Restore { get; set; }
    }

    public async Task OnGetAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            Response.Redirect("/Files");
            return;
        }

        var groups = await _groupService.GetGroupPermissionsAsync();
        Groups = groups.Select(g => new GroupPermissionViewModel
        {
            Id = g.Id,
            Name = g.Name,
            Read = g.AccessType.HasFlag(AccessType.Read),
            Write = g.AccessType.HasFlag(AccessType.Write),
            Delete = g.AccessType.HasFlag(AccessType.Delete),
            ManageAccess = g.AccessType.HasFlag(AccessType.ManageAccess),
            Restore = g.AccessType.HasFlag(AccessType.Restore)
        }).ToList();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (User.FindFirst("IsAdmin")?.Value != "True")
        {
            return Redirect("/Files");
        }

        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        foreach (var group in Groups)
        {
            var access = AccessType.None;
            if (group.Read) access |= AccessType.Read;
            if (group.Write) access |= AccessType.Write;
            if (group.Delete) access |= AccessType.Delete;
            if (group.ManageAccess) access |= AccessType.ManageAccess;
            if (group.Restore) access |= AccessType.Restore;

            await _groupService.SetGroupPermissionsAsync(group.Id, access, userId);
        }

        TempData["Success"] = "Права обновлены";
        return RedirectToPage();
    }
}
