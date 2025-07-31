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

        // Создаем администратора по умолчанию
        var admin = await userService.CreateUserAsync(
            email: "admin@filemanager.com",
            fullName: "Администратор",
            password: "admin123",
            department: "IT",
            isAdmin: true
        );

        // Создаем тестового пользователя
        await userService.CreateUserAsync(
            email: "user@filemanager.com",
            fullName: "Тестовый пользователь",
            password: "user123",
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
        var departments = new[] { "IT", "Бухгалтерия", "HR", "Общие документы" };

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

        Console.WriteLine("База данных инициализирована:");
        Console.WriteLine("Администратор: admin@filemanager.com / admin123");
        Console.WriteLine("Пользователь: user@filemanager.com / user123");
    }
}
