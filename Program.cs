using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 80 (default HTTP)
builder.WebHost.UseUrls("http://*:80", "http://*:5000");

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure IIS Integration
builder.WebHost.UseIISIntegration();


// Configure DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = null; // Session cookie - expires when browser closes
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add HttpContextAccessor for accessing session in views
builder.Services.AddHttpContextAccessor();

// Add services
builder.Services.AddControllersWithViews();
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ������� HTTPS

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
