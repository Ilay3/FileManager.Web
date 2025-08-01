using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager.Application.DTOs
{
    public class FileEditSessionDto
    {
        public Guid Id { get; set; }
        public Guid FileId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public string? IpAddress { get; set; }
        public bool IsActive { get; set; }
    }

}
