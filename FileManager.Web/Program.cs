using FileManager.Infrastructure.Extensions; // �����: �������� ���� using
using FileManager.Infrastructure.Data;
using FileManager.Application.Services;
using FileManager.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ��� ������� �������������� ����� �������!
builder.Services.AddInfrastructureServices(builder.Configuration);

// ��������� ��������������
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
    });

// ��������� �����������
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireClaim("IsAdmin", "True"));
});

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizePage("/Files/Index");
    options.Conventions.AuthorizePage("/Files");
    options.Conventions.AuthorizePage("/Admin", "AdminOnly");
});

// ��������� ��������� ������������ ��� API
builder.Services.AddControllers();

builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Middleware ��� ������������ ���������� �������������
app.UseUserActivity();

app.MapRazorPages();
app.MapControllers();

// ��������������� � �����
app.MapGet("/", async context =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        context.Response.Redirect("/Files");
    }
    else
    {
        context.Response.Redirect("/Account/Login");
    }
});

// ������������� ���� ������
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userService = scope.ServiceProvider.GetRequiredService<UserService>();

    try
    {
        await context.Database.EnsureCreatedAsync();
        await DatabaseInitializer.InitializeAsync(context, userService);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "������ ������������� ���� ������");
    }
}

app.Run();