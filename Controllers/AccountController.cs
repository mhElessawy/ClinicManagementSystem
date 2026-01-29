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

        public AccountController(ApplicationDbContext context, IFileProcessingService fileProcessingService)
        {
            _context = context;
            _loginService = new LoginService(context);
            _fileProcessingService = fileProcessingService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, redirect to home
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

            // Validate userType
            if (userType != "Admin" && userType != "Doctor" && userType != "Assistant")
            {
                ViewBag.Error = "Invalid user type selected";
                return View();
            }

            var result = await _loginService.AuthenticateAsync(username, password);

            if (result.Success)
            {
                // Check if selected userType matches actual userType
                if (result.UserType != userType)
                {
                    ViewBag.Error = $"Invalid credentials for {userType}. Please check your user type selection.";
                    return View();
                }

                // Set session
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

        [HttpGet]
        public IActionResult Register()
        {
            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DoctorInfo doctor, IFormFile? DoctorPictureFile, int? DepartmentId)
        {
            // Check if username already exists
            if (!string.IsNullOrEmpty(doctor.LoginUsername))
            {
                var existingDoctor = await _context.DoctorInfos
                    .FirstOrDefaultAsync(d => d.LoginUsername == doctor.LoginUsername);
                if (existingDoctor != null)
                {
                    ModelState.AddModelError("LoginUsername", "Username already exists");
                }
            }

            if (ModelState.IsValid)
            {
                // Handle image upload with resizing
                if (DoctorPictureFile != null && DoctorPictureFile.Length > 0)
                {
                    doctor.DoctorPicture = await _fileProcessingService.ResizeImageAsync(
                        DoctorPictureFile, maxWidth: 400, maxHeight: 400, quality: 75);
                }

                // Hash password if provided
                if (!string.IsNullOrEmpty(doctor.LoginPassword))
                {
                    try
                    {
                        doctor.LoginPassword = BCrypt.Net.BCrypt.HashPassword(doctor.LoginPassword);
                    }
                    catch
                    {
                        // Keep plain text if BCrypt fails
                    }
                }

                doctor.RegDate = DateTime.Now;
                doctor.Active = true;
                doctor.CanLogin = true;

                _context.Add(doctor);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Registration successful! You can now login.";
                return RedirectToAction("Login");
            }

            PopulateDropdowns(doctor.SpecialistId, DepartmentId);
            return View(doctor);
        }

        private void PopulateDropdowns(int? selectedSpecialist = null, int? selectedDepartment = null)
        {
            ViewBag.DepartmentId = new SelectList(_context.Departments, "Id", "DepartmentName", selectedDepartment);
            ViewBag.SpecialistId = new SelectList(_context.Specialists, "Id", "SpecialistName", selectedSpecialist);

            // Pass specialists data as JSON for JavaScript filtering
            var specialistsData = _context.Specialists.Select(s => new {
                id = s.Id,
                name = s.SpecialistName,
                departmentId = s.DepartmentId
            }).ToList();
            ViewBag.SpecialistsJson = System.Text.Json.JsonSerializer.Serialize(specialistsData);
        }
    }
}
