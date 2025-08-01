using FileManager.Application.Services;
using FileManager.Domain.Entities;
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
            }

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