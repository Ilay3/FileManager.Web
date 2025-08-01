using FileManager.Application.Services;
using FileManager.Domain.Entities;
using FileManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FileManager.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(AppDbContext context, UserService userService)
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

            // Создаем тестового пользователя
            var user = await userService.CreateUserAsync(
                email: "user@filemanager.com",
                fullName: "Тестовый пользователь",
                password: "User123!@#",
                department: "Общий отдел",
                isAdmin: false
            );

            // Создаем корневую папку
            var rootFolder = new Folder
            {
                Name = "Корневая папка",
                YandexPath = "/FileManager",
                CreatedById = admin.Id
            };

            context.Folders.Add(rootFolder);
            await context.SaveChangesAsync();

            // Создаем папки отделов
            var departments = new[] { "IT отдел", "Бухгалтерия", "HR отдел", "Общие документы" };
            var createdFolders = new Dictionary<string, Folder>();

            foreach (var dept in departments)
            {
                var folder = new Folder
                {
                    Name = dept,
                    YandexPath = $"/FileManager/{dept}",
                    ParentFolderId = rootFolder.Id,
                    CreatedById = admin.Id
                };

                context.Folders.Add(folder);
                createdFolders[dept] = folder;
            }

            await context.SaveChangesAsync();

            // Создаем тестовые файлы для демонстрации
            var testFiles = new[]
            {
                new Files
                {
                    Name = "Отчет по проекту.docx",
                    OriginalName = "Отчет по проекту.docx",
                    YandexPath = "/FileManager/IT отдел/Отчет по проекту.docx",
                    FileType = FileType.Document,
                    Extension = ".docx",
                    SizeBytes = 245760,
                    FolderId = createdFolders["IT отдел"].Id,
                    UploadedById = admin.Id,
                    Tags = "отчет,проект,итоговый"
                },
                new Files
                {
                    Name = "Бюджет 2024.xlsx",
                    OriginalName = "Бюджет 2024.xlsx",
                    YandexPath = "/FileManager/Бухгалтерия/Бюджет 2024.xlsx",
                    FileType = FileType.Spreadsheet,
                    Extension = ".xlsx",
                    SizeBytes = 89456,
                    FolderId = createdFolders["Бухгалтерия"].Id,
                    UploadedById = user.Id,
                    Tags = "бюджет,2024,финансы"
                },
                new Files
                {
                    Name = "Презентация продукта.pptx",
                    OriginalName = "Презентация продукта.pptx",
                    YandexPath = "/FileManager/Общие документы/Презентация продукта.pptx",
                    FileType = FileType.Presentation,
                    Extension = ".pptx",
                    SizeBytes = 1024000,
                    FolderId = createdFolders["Общие документы"].Id,
                    UploadedById = admin.Id,
                    Tags = "презентация,продукт,маркетинг"
                },
                new Files
                {
                    Name = "Инструкция пользователя.pdf",
                    OriginalName = "Инструкция пользователя.pdf",
                    YandexPath = "/FileManager/Общие документы/Инструкция пользователя.pdf",
                    FileType = FileType.Pdf,
                    Extension = ".pdf",
                    SizeBytes = 512000,
                    FolderId = createdFolders["Общие документы"].Id,
                    UploadedById = user.Id,
                    Tags = "инструкция,руководство,пользователь"
                },
                new Files
                {
                    Name = "Заметки.txt",
                    OriginalName = "Заметки.txt",
                    YandexPath = "/FileManager/IT отдел/Заметки.txt",
                    FileType = FileType.Text,
                    Extension = ".txt",
                    SizeBytes = 2048,
                    FolderId = createdFolders["IT отдел"].Id,
                    UploadedById = admin.Id,
                    Tags = "заметки,todo"
                },
                new Files
                {
                    Name = "Логотип компании.png",
                    OriginalName = "Логотип компании.png",
                    YandexPath = "/FileManager/Общие документы/Логотип компании.png",
                    FileType = FileType.Image,
                    Extension = ".png",
                    SizeBytes = 156789,
                    FolderId = createdFolders["Общие документы"].Id,
                    UploadedById = admin.Id,
                    Tags = "логотип,брендинг,дизайн"
                },
                new Files
                {
                    Name = "Архив документов.zip",
                    OriginalName = "Архив документов.zip",
                    YandexPath = "/FileManager/HR отдел/Архив документов.zip",
                    FileType = FileType.Archive,
                    Extension = ".zip",
                    SizeBytes = 5242880,
                    FolderId = createdFolders["HR отдел"].Id,
                    UploadedById = user.Id,
                    Tags = "архив,документы,hr"
                }
            };

            context.Files.AddRange(testFiles);
            await context.SaveChangesAsync();

            Console.WriteLine("=== База данных инициализирована ===");
            Console.WriteLine("Администратор:");
            Console.WriteLine("  Email: admin@filemanager.com");
            Console.WriteLine("  Пароль: Admin123!@#");
            Console.WriteLine();
            Console.WriteLine("Тестовый пользователь:");
            Console.WriteLine("  Email: user@filemanager.com");
            Console.WriteLine("  Пароль: User123!@#");
            Console.WriteLine();
            Console.WriteLine($"Создано папок: {departments.Length + 1}");
            Console.WriteLine($"Создано тестовых файлов: {testFiles.Length}");
            Console.WriteLine();
            Console.WriteLine("ВАЖНО: Смените пароли после первого входа!");
            Console.WriteLine("=====================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка инициализации БД: {ex.Message}");
            throw;
        }
    }
}