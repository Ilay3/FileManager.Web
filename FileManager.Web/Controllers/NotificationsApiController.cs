using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsApiController : ControllerBase
{
    private readonly IEmailService _emailService;

    public NotificationsApiController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] NotificationRequest request)
    {
        await _emailService.SendFileChangeNotificationAsync(request.Recipients, request.Description);
        return Ok();
    }

    public class NotificationRequest
    {
        public List<string> Recipients { get; set; } = new();
        public string Description { get; set; } = string.Empty;
    }
}
