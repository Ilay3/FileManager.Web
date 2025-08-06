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
    FilePreview,    // предпросмотр файла
    FileOpenForEdit, // открытие в редакторе

    // Операции с папками
    FolderCreate,
    FolderDelete,
    FolderRename,
    FolderMove,
    FolderRestore,

    // Управление доступом
    AccessGranted,
    AccessRevoked,
    AccessChanged,

    // Системные события
    Error,
    UnauthorizedAccess
}