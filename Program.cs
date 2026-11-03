using System.Globalization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyLichHoc.Data;

var builder = WebApplication.CreateBuilder(args);

// ---- Cơ sở dữ liệu: Sqlite (mặc định) hoặc SqlServer, đổi trong appsettings.json ----
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
builder.Services.AddDbContext<AppDbContext>(o =>
{
    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
        o.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer"));
    else
        o.UseSqlite(builder.Configuration.GetConnectionString("Sqlite"));
});

// ---- MVC + chống CSRF cho mọi yêu cầu POST/PUT/DELETE ----
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddLocalization(o => o.ResourcesPath = "Resources");

// ---- Session + Cookie xác thực ----
builder.Services.AddSession(o => { o.IdleTimeout = TimeSpan.FromMinutes(30); o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; });
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // đặt Always khi triển khai HTTPS
    o.ExpireTimeSpan = TimeSpan.FromDays(7);
    o.SlidingExpiration = true;
    o.Events.OnRedirectToLogin = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 401; else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
    o.Events.OnRedirectToAccessDenied = ctx =>
    {
        if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.StatusCode = 403; else ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    };
});
builder.Services.AddAuthorization();


var app = builder.Build();

// ---- Tạo CSDL + dữ liệu mẫu ở lần chạy đầu tiên ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase) && DbSeeder.CoDuLieuLech(db))
    {
        // CSDL SQLite cũ bị lệch khóa ngoại: sao lưu rồi tạo lại từ dữ liệu mẫu
        try
        {
            var cs = builder.Configuration.GetConnectionString("Sqlite") ?? "";
            var file = cs.Split(';').Select(x => x.Split('=')).Where(x => x.Length == 2 && x[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase)).Select(x => x[1].Trim()).FirstOrDefault();
            if (!string.IsNullOrEmpty(file) && File.Exists(file)) File.Copy(file, file + ".bak", true);
        }
        catch { /* không sao lưu được thì vẫn tiếp tục */ }
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
        Console.WriteLine("[QuanLyLichHoc] Phát hiện CSDL cũ bị lệch dữ liệu -> đã sao lưu (.bak) và tạo lại từ dữ liệu mẫu.");
    }
    DbSeeder.Seed(db);
    DbSeeder.Repair(db);
}

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStaticFiles();
app.UseRouting();

// ---- Đa ngôn ngữ: vi-VN (mặc định) / en-US, lưu trong cookie ----
var cultures = new[] { new CultureInfo("vi-VN"), new CultureInfo("en-US") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi-VN"),
    SupportedCultures = cultures,
    SupportedUICultures = cultures,
    RequestCultureProviders = new List<Microsoft.AspNetCore.Localization.IRequestCultureProvider> { new CookieRequestCultureProvider() }
});

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
