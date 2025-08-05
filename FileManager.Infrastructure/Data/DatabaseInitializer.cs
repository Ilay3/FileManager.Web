using FileManager.Application.Services;
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

}
