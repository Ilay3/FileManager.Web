using FileManager.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace FileManager.Domain.Entities;

public class Folder : BaseEntity
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string YandexPath { get; set; } = string.Empty; // полный путь в Яндекс.Диске

    // Древовидная структура
    public Guid? ParentFolderId { get; set; }
    public virtual Folder? ParentFolder { get; set; }
    public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();

    // Кто создал
    public Guid CreatedById { get; set; }
    public virtual User CreatedBy { get; set; } = null!;

    // Navigation properties
    public virtual ICollection<Files> Files { get; set; } = new List<Files>();
    public virtual ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();
}
