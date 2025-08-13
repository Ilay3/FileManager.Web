using FileManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace FileManager.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class NotificationsApiController : ControllerBase
{
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationsApiController> _logger;

    public NotificationsApiController(IEmailService emailService, ILogger<NotificationsApiController> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Send([FromBody] NotificationRequest request)
    {
        try
        {
            var failedRecipients = await _emailService.SendFileChangeNotificationAsync(request.Recipients, request.Description);
            if (failedRecipients.Count > 0)
            {
                _logger.LogWarning("Не удалось отправить уведомления на адреса: {Emails}", string.Join(", ", failedRecipients));
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке уведомлений");
            return StatusCode(500, "Не удалось отправить уведомления");
        }
    }

    public class NotificationRequest
    {
        public List<string> Recipients { get; set; } = new();
        public string Description { get; set; } = string.Empty;
    }
}
