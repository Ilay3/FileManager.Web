using FileManager.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Domain.Entities;

public class Favorite : BaseEntity
{
    [Required]
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Guid? FileId { get; set; }
    public virtual Files? File { get; set; }

    public Guid? FolderId { get; set; }
    public virtual Folder? Folder { get; set; }
}
