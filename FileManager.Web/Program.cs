using FileManager.Infrastructure.Extensions;
using FileManager.Infrastructure.Data;
using FileManager.Application.Services;
using FileManager.Web.Middleware;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http;

using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});

builder.Host.UseWindowsService(o => o.ServiceName = "FileManager Service");

builder.Services.AddInfrastructureServices(builder.Configuration);

// Аутентификация
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "FileManager.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"results\":[],\"error\":\"Unauthorized\"}");
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"results\":[],\"error\":\"Forbidden\"}");
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

// Авторизация
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("IsAdmin", "True"));
});

// Razor + Controllers
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Files/Index");
    options.Conventions.AuthorizePage("/Files");
    options.Conventions.AuthorizePage("/Admin", "AdminOnly");
    options.Conventions.AuthorizePage("/Account/Profile");
});
builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 1024L * 1024 * 1024; // 1 ГБ
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// редирект на HTTPS — только если включено в конфиге
var enforceHttps = app.Configuration.GetValue<bool>("EnforceHttps");
if (enforceHttps)
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware активности пользователей
app.UseUserActivity();

app.MapRazorPages();
app.MapControllers();

// Инициализация БД
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();
    try
    {
        await context.Database.MigrateAsync();
        await DatabaseInitializer.InitializeAsync(context, userService);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка инициализации базы данных");
    }
}

app.Run();
