using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string email, string resetToken, string userName);
        Task SendAccountLockedEmailAsync(string email, string userName, string reason);
        Task SendWelcomeEmailAsync(string email, string userName, string temporaryPassword);
        Task SendEmailConfirmationAsync(string email, string userName, string code);
    }

}
