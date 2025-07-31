namespace FileManager.Domain.Enums;

public enum AuditAction
{
    // Аутентификация
    Login,
    Logout,

    // Операции с файлами
    FileUpload,
    FileDownload,
    FileView,
    FileEdit,
    FileDelete,
    FileRestore,    // откат к версии

    // Операции с папками
    FolderCreate,
    FolderDelete,
    FolderRename,
    FolderMove,

    // Управление доступом
    AccessGranted,
    AccessRevoked,
    AccessChanged,

    // Системные события
    Error,
    UnauthorizedAccess
}
