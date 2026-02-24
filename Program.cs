using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add File Processing Service for image resizing
builder.Services.AddScoped<IFileProcessingService, FileProcessingService>();

// Configure Email Settings
builder.Services.Configure<ClinicManagementSystem.Models.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();


// Configure DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = null;
});

// Add HttpContextAccessor for accessing session in views
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Apply pending database migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    // Ensure Clinic Manager role exists (seed if missing)
    if (!db.Roles.Any(r => r.Id == 6))
    {
        db.Database.ExecuteSqlRaw(@"
            SET IDENTITY_INSERT [Roles] ON;
            INSERT INTO [Roles] ([Id],[Active],[CanManageAssistants],[CanManageDepartments],[CanManageDiagnoses],[CanManageDoctors],[CanManagePatients],[CanManageSpecialists],[CanManageUsers],[CanViewReports],[Description],[RoleName],[ViewAllPatients],[ViewOwnPatientsOnly])
            VALUES (6,1,1,0,0,1,0,0,0,1,N'Manages a group of doctors - can add doctors, view income, manage assistants and receptionists',N'Clinic Manager',0,0);
            SET IDENTITY_INSERT [Roles] OFF;");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    //pattern: "{controller=Account}/{action=Login}/{id?}");
    pattern: "{controller=Account}/{action=Welcome}/{id?}");

app.Run();
