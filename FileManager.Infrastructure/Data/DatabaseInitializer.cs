using FileManager.Application.Services;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using FileManager.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace FileManager.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context, UserService userService, IYandexDiskService yandexDiskService)
    {
        // Проверяем, есть ли пользователи
        if (await context.Users.AnyAsync())
            return;

        try
        {
            // Создаем администратора по умолчанию
            var admin = await userService.CreateUserAsync(
                email: "admin@filemanager.com",
                fullName: "Системный администратор",
                password: "Admin123!@#",
                department: "IT отдел",
                isAdmin: true
            );

            // Создаем корневую папку без тестовых данных
            var rootFolder = new Folder
            {
                Name = "Корневая папка",
                YandexPath = "/FileManager",
                CreatedById = admin.Id
            };

            context.Folders.Add(rootFolder);
            await context.SaveChangesAsync();

            await SyncDiskAsync(context, admin, rootFolder, yandexDiskService);

            Console.WriteLine("=== База данных инициализирована ===");
            Console.WriteLine("Администратор:");
            Console.WriteLine("  Email: admin@filemanager.com");
            Console.WriteLine("  Пароль: Admin123!@#");
            Console.WriteLine();
            Console.WriteLine("ВАЖНО: Смените пароль после первого входа!");
            Console.WriteLine("=====================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка инициализации БД: {ex.Message}");
            throw;
        }
    }

    private static async Task SyncDiskAsync(AppDbContext context, User admin, Folder root, IYandexDiskService yandexDiskService)
    {
        async Task Traverse(string path, Folder parent)
        {
            var items = await yandexDiskService.GetFolderContentsAsync(path);

            foreach (var item in items)
            {
                if (item.IsDirectory)
                {
                    var folder = new Folder
                    {
                        Name = item.Name,
                        YandexPath = item.Path,
                        ParentFolderId = parent.Id,
                        CreatedById = admin.Id
                    };

                    context.Folders.Add(folder);
                    await context.SaveChangesAsync();

                    await Traverse(item.Path, folder);
                }
                else
                {
                    var extension = Path.GetExtension(item.Name).ToLowerInvariant();
                    var file = new Files
                    {
                        Name = Path.GetFileNameWithoutExtension(item.Name),
                        OriginalName = item.Name,
                        Extension = extension,
                        YandexPath = item.Path,
                        FileType = DetermineFileType(extension),
                        SizeBytes = item.Size,
                        FolderId = parent.Id,
                        UploadedById = admin.Id
                    };

                    context.Files.Add(file);
                }
            }

            await context.SaveChangesAsync();
        }

        await Traverse(root.YandexPath, root);
    }

    private static FileType DetermineFileType(string extension) => extension switch
    {
        ".doc" or ".docx" => FileType.Document,
        ".xls" or ".xlsx" => FileType.Spreadsheet,
        ".ppt" or ".pptx" => FileType.Presentation,
        ".pdf" => FileType.Pdf,
        ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => FileType.Image,
        ".txt" => FileType.Text,
        ".zip" or ".rar" or ".7z" => FileType.Archive,
        _ => FileType.Other
    };
}
