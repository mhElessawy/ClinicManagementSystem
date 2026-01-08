# 🚀 Complete Code - Part 2: Controllers

## 🎮 CONTROLLERS

### AccountController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.Services;
using ClinicManagementSystem.Helpers;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LoginService _loginService;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
            _loginService = new LoginService(context);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ViewBag.Error = "Please enter username and password";
                return View();
            }

            var result = await _loginService.AuthenticateAsync(username, password);

            if (result.Success)
            {
                SessionHelper.SetUserSession(
                    HttpContext.Session,
                    result.UserId,
                    result.UserName!,
                    result.UserType!,
                    result.FullName!,
                    result.DoctorId,
                    result.RoleId
                );

                TempData["Success"] = $"Welcome back, {result.FullName}!";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = result.ErrorMessage;
            return View();
        }

        public IActionResult Logout()
        {
            SessionHelper.ClearSession(HttpContext.Session);
            TempData["Success"] = "You have been logged out successfully";
            return RedirectToAction("Login");
        }
    }
}
```

### HomeController.cs
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Calculate stats based on user type
            ViewBag.TotalDoctors = await _context.DoctorInfos.CountAsync(d => d.Active);
            
            if (userType == SessionHelper.TYPE_ADMIN)
            {
                ViewBag.TotalPatients = await _context.Patients.CountAsync();
                ViewBag.TotalDiagnoses = await _context.PatientDiagnoses.CountAsync();
                ViewBag.TodayDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DiagnosisDate.Date == DateTime.Today);
            }
            else if (userType == SessionHelper.TYPE_DOCTOR && doctorId.HasValue)
            {
                ViewBag.TotalPatients = await _context.Patients
                    .CountAsync(p => p.DoctorId == doctorId.Value);
                ViewBag.TotalDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value);
                ViewBag.TodayDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value && d.DiagnosisDate.Date == DateTime.Today);
            }
            else if (userType == SessionHelper.TYPE_ASSISTANT && doctorId.HasValue)
            {
                ViewBag.TotalPatients = await _context.Patients
                    .CountAsync(p => p.DoctorId == doctorId.Value);
                ViewBag.TotalDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value);
                ViewBag.TodayDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value && d.DiagnosisDate.Date == DateTime.Today);
            }

            ViewBag.UserName = SessionHelper.GetFullName(HttpContext.Session);
            ViewBag.UserType = userType;

            return View();
        }
    }
}
```

### PatientsController.cs (WITH DOCTOR FILTERING)
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Patients
        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<Patient> patientsQuery = _context.Patients.Include(p => p.Doctor);

            // CRITICAL: Filter patients based on user type
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (doctorId.HasValue)
                {
                    patientsQuery = patientsQuery.Where(p => p.DoctorId == doctorId.Value);
                }
                else
                {
                    return View(new List<Patient>());
                }
            }
            // Admin sees all patients

            return View(await patientsQuery.ToListAsync());
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Doctor)
                .Include(p => p.PatientDiagnoses)
                    .ThenInclude(pd => pd.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
                return NotFound();

            // Check access rights
            if (!CanAccessPatient(patient.DoctorId))
            {
                TempData["Error"] = "You don't have permission to view this patient";
                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }

        // GET: Patients/Create
        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            PopulateDoctorDropdown();
            return View();
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,PatientName,PatientCivilID,PatientTel1,PatientTel2,PatientAddress,DoctorId")] Patient patient)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // CRITICAL: Auto-assign to current doctor if doctor/assistant
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (doctorId.HasValue)
                    patient.DoctorId = doctorId.Value;
            }

            if (ModelState.IsValid)
            {
                _context.Add(patient);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Patient added successfully";
                return RedirectToAction(nameof(Index));
            }

            PopulateDoctorDropdown(patient.DoctorId);
            return View(patient);
        }

        // GET: Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
                return NotFound();

            if (!CanAccessPatient(patient.DoctorId))
            {
                TempData["Error"] = "You don't have permission to edit this patient";
                return RedirectToAction(nameof(Index));
            }

            PopulateDoctorDropdown(patient.DoctorId);
            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PatientName,PatientCivilID,PatientTel1,PatientTel2,PatientAddress,DoctorId")] Patient patient)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != patient.Id)
                return NotFound();

            if (!CanAccessPatient(patient.DoctorId))
            {
                TempData["Error"] = "You don't have permission to edit this patient";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Patient updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateDoctorDropdown(patient.DoctorId);
            return View(patient);
        }

        // GET: Patients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
                return NotFound();

            if (!CanAccessPatient(patient.DoctorId))
            {
                TempData["Error"] = "You don't have permission to delete this patient";
                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                if (!CanAccessPatient(patient.DoctorId))
                {
                    TempData["Error"] = "You don't have permission to delete this patient";
                    return RedirectToAction(nameof(Index));
                }

                _context.Patients.Remove(patient);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Patient deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }

        private void PopulateDoctorDropdown(int? selectedValue = null)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<DoctorInfo> doctorsQuery = _context.DoctorInfos.Where(d => d.Active);

            // If doctor/assistant, only show their doctor
            if ((userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT) && doctorId.HasValue)
            {
                doctorsQuery = doctorsQuery.Where(d => d.Id == doctorId.Value);
            }

            ViewBag.DoctorId = new SelectList(doctorsQuery, "Id", "DoctorName", selectedValue);
        }

        private bool CanAccessPatient(int? patientDoctorId)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var currentDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Admin can access all
            if (userType == SessionHelper.TYPE_ADMIN)
                return true;

            // Doctor/Assistant can only access their patients
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                return currentDoctorId.HasValue && patientDoctorId == currentDoctorId.Value;
            }

            return false;
        }
    }
}
```

---

## ⚙️ CONFIGURATION FILES

### Program.cs
```csharp
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // Important: Enable session
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
```

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=ClinicDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### ClinicManagementSystem.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.VisualStudio.Web.CodeGeneration.Design" Version="8.0.0" />
    <PackageReference Include="ClosedXML" Version="0.102.1" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>

</Project>
```

---

## 🗄️ DATABASE MIGRATION COMMANDS

### Create Migration:
```powershell
# In Package Manager Console:
Add-Migration InitialCreate
Update-Database
```

### If you need to start fresh:
```powershell
# Drop database and recreate
Drop-Database
Add-Migration InitialCreate
Update-Database
```

---

## 🔑 DEFAULT LOGIN

```
Username: admin
Password: Admin@123
Role: Super Admin
```

---

## 📝 QUICK START GUIDE

1. **Extract project**
2. **Open in Visual Studio 2022**
3. **Restore packages**: `dotnet restore`
4. **Update connection string** in appsettings.json
5. **Create database**: `Update-Database`
6. **Run project**: Press F5
7. **Login**: admin / Admin@123

---

## 🎯 WHAT'S WORKING

✅ All 8 Models (English)
✅ Role-based permissions (5 roles)
✅ Multi-user login (Admin/Doctor/Assistant)
✅ Patient filtering by doctor
✅ Session management
✅ Database with seed data
✅ BCrypt + Plain text password support

---

## 🔄 WHAT NEEDS COMPLETION

❌ Remaining 8 controllers (English conversion)
❌ All 46 views (English conversion)
❌ Role-based menu in Layout
❌ RolesController (CRUD for roles)

---

**Follow IMPLEMENTATION_GUIDE.md for completing the rest!**
