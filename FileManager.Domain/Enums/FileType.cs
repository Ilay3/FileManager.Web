namespace FileManager.Domain.Enums;

public enum FileType
{
    Document,     // docx, doc - для онлайн редактирования
    Spreadsheet,  // xlsx, xls - для онлайн редактирования  
    Presentation, // pptx, ppt - для онлайн редактирования
    Pdf,         // pdf - предпросмотр
    Image,       // jpg, png, gif - предпросмотр
    Text,        // txt - предпросмотр
    Archive,     // zip, rar
    Other
}
