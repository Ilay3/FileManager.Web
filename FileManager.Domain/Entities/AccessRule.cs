using FileManager.Domain.Common;
using FileManager.Domain.Enums;

namespace FileManager.Domain.Entities;

public class AccessRule : BaseEntity
{
    // К чему применяется правило (файл ИЛИ папка)
    public Guid? FileId { get; set; }
    public virtual Files? File { get; set; }

    public Guid? FolderId { get; set; }
    public virtual Folder? Folder { get; set; }

    // Кому дается доступ (пользователь ИЛИ группа)
    public Guid? UserId { get; set; }
    public virtual User? User { get; set; }

    public Guid? GroupId { get; set; }
    public virtual Group? Group { get; set; }

    public AccessType AccessType { get; set; }

    // Наследование прав от папки к файлам (если не переопределено)
    public bool InheritFromParent { get; set; } = true;

    // Кто назначил права
    public Guid GrantedById { get; set; }
    public virtual User GrantedBy { get; set; } = null!;
}
