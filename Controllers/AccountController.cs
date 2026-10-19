using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Services;
using ClinicManagementSystem.Helpers;
using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LoginService _loginService;
        private readonly IFileProcessingService _fileProcessingService;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(ApplicationDbContext context, IFileProcessingService fileProcessingService, IEmailService emailService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _loginService = new LoginService(context);
            _fileProcessingService = fileProcessingService;
            _emailService = emailService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Account/Welcome
        public IActionResult Welcome()
        {
            if (SessionHelper.IsLoggedIn(HttpContext.Session))
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // Clinic registration replaces public individual-doctor registration.
        [HttpGet]
        public IActionResult Register() => RedirectToAction(nameof(RegisterClinic));

        [HttpPost, ValidateAntiForgeryToken]
        public IActionResult Register(DoctorInfo doctor) => RedirectToAction(nameof(RegisterClinic));

        [HttpGet]
        public IActionResult RegisterClinic() => View(new ClinicRegistration());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterClinic(ClinicRegistration registration)
        {
            if (!ModelState.IsValid) return View(registration);
            var clinic = new Clinic
            {
                Name = registration.Name.Trim(),
                Address = registration.Address,
                Phone = registration.Phone,
                Email = registration.Email,
                Code = "CL-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()
            };
            var group = new DoctorGroup { GroupName = clinic.Name, Clinic = clinic };
            var manager = new UserInfo
            {
                UserName = registration.Username.Trim(),
                UserFullName = registration.ManagerName,
                UserPassword = BCrypt.Net.BCrypt.HashPassword(registration.Password),
                UserTel = registration.Phone,
                RoleId = 6,
                Clinic = clinic,
                Group = group,
                Active = true
            };
            await _context.RegisterClinicAsync(clinic, group, manager);
            TempData["Success"] = $"Clinic registered successfully. Save your clinic code: {clinic.Code} — use it with your username and password to log in as Clinic Manager.";
            return RedirectToAction(nameof(Login));
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
        public async Task<IActionResult> Login(string username, string password, string userType, string? clinicCode)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(userType))
            {
                ViewBag.Error = "Please fill all fields";
                return View();
            }

            if (userType != "Admin" && userType != "Doctor" && userType != "Assistant" && userType != "Reception" && userType != "ClinicManager")
            {
                ViewBag.Error = "Invalid user type selected";
                return View();
            }

            var result = await _loginService.AuthenticateAsync(username, password, userType, clinicCode);

            if (result.Success)
            {
                if (result.UserType != userType)
                {
                    ViewBag.Error = $"Invalid credentials for {userType}.";
                    return View();
                }

                SessionHelper.SetUserSession(
                    HttpContext.Session,
                    result.UserId,
                    result.UserName!,
                    result.UserType!,
                    result.FullName!,
                    result.DoctorId,
                    result.RoleId,
                    result.GroupId
                );

                if (result.ClinicId.HasValue)
                    HttpContext.Session.SetInt32("ClinicId", result.ClinicId.Value);
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

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email, string clinicCode)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please enter your email address";
                return View();
            }

            var code = clinicCode?.Trim().ToUpperInvariant();
            var doctor = await _context.DoctorInfos.IgnoreQueryFilters().FirstOrDefaultAsync(d =>
                d.Email == email && d.Active && d.CanLogin && d.Clinic != null && d.Clinic.Active && d.Clinic.Code == code);

            if (doctor == null)
            {
                // Don't reveal that the email doesn't exist for security reasons
                TempData["Success"] = "If an account with that email exists, a password reset link has been sent.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Generate password reset token
            var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            doctor.PasswordResetToken = token;
            doctor.PasswordResetTokenExpiry = DateTime.Now.AddHours(1); // Token valid for 1 hour

            await _context.DoctorInfos.IgnoreQueryFilters().Where(d => d.Id == doctor.Id && d.Active && d.Clinic != null && d.Clinic.Active)
                .ExecuteUpdateAsync(update => update.SetProperty(d => d.PasswordResetToken, token)
                    .SetProperty(d => d.PasswordResetTokenExpiry, (DateTime?)DateTime.Now.AddHours(1)));

            // Generate reset link
            var resetLink = Url.Action("ResetPassword", "Account",
                new { token = token }, Request.Scheme);

            // Send email
            try
            {
                await _emailService.SendPasswordResetEmailAsync(email, resetLink!, doctor.DoctorName);
                TempData["Success"] = "If an account with that email exists, a password reset link has been sent.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send password reset email: {ex.Message}");
                ViewBag.Error = "Failed to send password reset email. Please try again later.";
                return View();
            }

            return RedirectToAction(nameof(ForgotPassword));
        }

        // GET: Account/ResetPassword
        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(Login));
            }

            var doctor = await _context.DoctorInfos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Active && d.Clinic != null && d.Clinic.Active && d.PasswordResetToken == token &&
                                         d.PasswordResetTokenExpiry > DateTime.Now);

            if (doctor == null)
            {
                TempData["Error"] = "Invalid or expired password reset link.";
                return RedirectToAction(nameof(Login));
            }

            ViewBag.Token = token;
            return View();
        }

        // POST: Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(Login));
            }

            ViewBag.Token = token;

            if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Please fill in all fields";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match";
                return View();
            }

            if (newPassword.Length < 8 || newPassword.Length > 72)
            {
                ViewBag.Error = "Password must be between 8 and 72 characters long";
                return View();
            }

            var doctor = await _context.DoctorInfos.IgnoreQueryFilters()
                .FirstOrDefaultAsync(d => d.Active && d.Clinic != null && d.Clinic.Active && d.PasswordResetToken == token &&
                                         d.PasswordResetTokenExpiry > DateTime.Now);

            if (doctor == null)
            {
                TempData["Error"] = "Invalid or expired password reset link.";
                return RedirectToAction(nameof(Login));
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var changed = await _context.DoctorInfos.IgnoreQueryFilters().Where(d =>
                d.Id == doctor.Id && d.PasswordResetToken == token && d.PasswordResetTokenExpiry > DateTime.Now &&
                d.Active && d.Clinic != null && d.Clinic.Active)
                .ExecuteUpdateAsync(update => update.SetProperty(d => d.LoginPassword, hash)
                    .SetProperty(d => d.PasswordResetToken, (string?)null)
                    .SetProperty(d => d.PasswordResetTokenExpiry, (DateTime?)null));
            if (changed != 1)
            {
                TempData["Error"] = "Invalid or expired password reset link.";
                return RedirectToAction(nameof(Login));
            }
            TempData["Success"] = "Your password has been reset successfully. You can now login with your new password.";
            return RedirectToAction(nameof(Login));
        }

        private void PopulateDropdowns(int? selectedSpecialist = null, int? selectedDepartment = null)
        {
            ViewBag.DepartmentId = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "Id", "DepartmentName", selectedDepartment);
            ViewBag.Specialists = new SelectList(_context.Specialists.OrderBy(d => d.SpecialistName), "Id", "SpecialistName", selectedSpecialist);

            var specialistsData = _context.Specialists.OrderBy(d => d.SpecialistName).Select(s => new {
                id = s.Id,
                name = s.SpecialistName,
                departmentId = s.DepartmentId
            }).ToList();
            ViewBag.SpecialistsJson = System.Text.Json.JsonSerializer.Serialize(specialistsData);
        }
    }
}