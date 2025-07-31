namespace FileManager.Domain.Enums;

[Flags]
public enum AccessType
{
    None = 0,
    Read = 1,              // читать, просматривать
    Write = 2,             // редактировать
    Delete = 4,            // удалять
    ManageAccess = 8,      // изменять права доступа
    Restore = 16,          // откат к версии
    FullAccess = Read | Write | Delete | ManageAccess | Restore
}
