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

        public AccountController(ApplicationDbContext context, IFileProcessingService fileProcessingService, IEmailService emailService)
        {
            _context = context;
            _loginService = new LoginService(context);
            _fileProcessingService = fileProcessingService;
            _emailService = emailService;
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

        // GET: Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            PopulateDropdowns();
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DoctorInfo doctor, string ConfirmPassword, IFormFile? DoctorPictureFile, int? DepartmentId)
        {
            // Check if Civil ID already exists
            if (!string.IsNullOrEmpty(doctor.DoctorCivilId))
            {
                var existingCivilId = await _context.DoctorInfos
                    .AnyAsync(d => d.DoctorCivilId == doctor.DoctorCivilId);
                if (existingCivilId)
                {
                    ModelState.AddModelError("DoctorCivilId", "This Civil ID is already registered");
                }
            }

            // Check if Email already exists
            if (!string.IsNullOrEmpty(doctor.Email))
            {
                var existingEmail = await _context.DoctorInfos
                    .AnyAsync(d => d.Email == doctor.Email);
                if (existingEmail)
                {
                    ModelState.AddModelError("Email", "This Email is already registered");
                }
            }

            // Check if Username already exists
            if (!string.IsNullOrEmpty(doctor.LoginUsername))
            {
                var existingUsername = await _context.DoctorInfos
                    .AnyAsync(d => d.LoginUsername == doctor.LoginUsername);
                if (existingUsername)
                {
                    ModelState.AddModelError("LoginUsername", "This Username is already taken");
                }
            }

            // Check password confirmation
            if (doctor.LoginPassword != ConfirmPassword)
            {
                ModelState.AddModelError("ConfirmPassword", "Passwords do not match");
            }

            // Validate required fields
            if (string.IsNullOrEmpty(doctor.DoctorCivilId))
                ModelState.AddModelError("DoctorCivilId", "Civil ID is required");

            if (string.IsNullOrEmpty(doctor.Email))
                ModelState.AddModelError("Email", "Email is required");

            if (string.IsNullOrEmpty(doctor.LoginUsername))
                ModelState.AddModelError("LoginUsername", "Username is required");

            if (string.IsNullOrEmpty(doctor.LoginPassword))
                ModelState.AddModelError("LoginPassword", "Password is required");

            if (ModelState.IsValid)
            {
                // Handle image upload with resizing
                if (DoctorPictureFile != null && DoctorPictureFile.Length > 0)
                {
                    doctor.DoctorPicture = await _fileProcessingService.ResizeImageAsync(
                        DoctorPictureFile, maxWidth: 400, maxHeight: 400, quality: 75);
                }

                // Hash password
                if (!string.IsNullOrEmpty(doctor.LoginPassword))
                {
                    doctor.LoginPassword = BCrypt.Net.BCrypt.HashPassword(doctor.LoginPassword);
                }

                doctor.CanLogin = true;
                doctor.Active = false; // Requires admin approval
                doctor.RegDate = DateTime.Now;

                _context.Add(doctor);
                await _context.SaveChangesAsync();

                // Send notification email to admin
                try
                {
                    await _emailService.SendDoctorRegistrationNotificationAsync(doctor);
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail the registration
                    Console.WriteLine($"Failed to send admin notification email: {ex.Message}");
                }

                TempData["Success"] = "Registration successful! Please wait for admin approval.";
                return RedirectToAction(nameof(Login));
            }

            PopulateDropdowns(doctor.SpecialistId, DepartmentId);
            return View(doctor);
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
        public async Task<IActionResult> Login(string username, string password, string userType)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(userType))
            {
                ViewBag.Error = "Please fill all fields";
                return View();
            }

            if (userType != "Admin" && userType != "Doctor" && userType != "Assistant" && userType != "Reception")
            {
                ViewBag.Error = "Invalid user type selected";
                return View();
            }

            var result = await _loginService.AuthenticateAsync(username, password);

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

        // GET: Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Please enter your email address";
                return View();
            }

            var doctor = await _context.DoctorInfos.FirstOrDefaultAsync(d => d.Email == email);

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

            await _context.SaveChangesAsync();

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

            var doctor = await _context.DoctorInfos
                .FirstOrDefaultAsync(d => d.PasswordResetToken == token &&
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

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters long";
                return View();
            }

            var doctor = await _context.DoctorInfos
                .FirstOrDefaultAsync(d => d.PasswordResetToken == token &&
                                         d.PasswordResetTokenExpiry > DateTime.Now);

            if (doctor == null)
            {
                TempData["Error"] = "Invalid or expired password reset link.";
                return RedirectToAction(nameof(Login));
            }

            // Update password
            doctor.LoginPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            doctor.PasswordResetToken = null;
            doctor.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();

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
