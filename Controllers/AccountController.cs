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

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
            _loginService = new LoginService(context);
        }

        // GET: Account/Welcome
        public IActionResult Welcome()
        {
            // If already logged in, redirect to home
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
            ViewBag.Specialists = new SelectList(_context.Specialists, "Id", "SpecialistName");
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(DoctorInfo doctor, string ConfirmPassword)
        {
            ViewBag.Specialists = new SelectList(_context.Specialists, "Id", "SpecialistName", doctor.SpecialistId);

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

            // Validate required fields for registration
            if (string.IsNullOrEmpty(doctor.DoctorCivilId))
            {
                ModelState.AddModelError("DoctorCivilId", "Civil ID is required for registration");
            }

            if (string.IsNullOrEmpty(doctor.Email))
            {
                ModelState.AddModelError("Email", "Email is required for registration");
            }

            if (string.IsNullOrEmpty(doctor.LoginUsername))
            {
                ModelState.AddModelError("LoginUsername", "Username is required for registration");
            }

            if (string.IsNullOrEmpty(doctor.LoginPassword))
            {
                ModelState.AddModelError("LoginPassword", "Password is required for registration");
            }

            if (ModelState.IsValid)
            {
                // Hash password
                if (!string.IsNullOrEmpty(doctor.LoginPassword))
                {
                    doctor.LoginPassword = BCrypt.Net.BCrypt.HashPassword(doctor.LoginPassword);
                }

                // Set default values
                doctor.CanLogin = true;
                doctor.Active = false; // Requires admin approval
                doctor.RegDate = DateTime.Now;

                _context.Add(doctor);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Registration successful! Please wait for admin approval before logging in.";
                return RedirectToAction(nameof(Login));
            }

            return View(doctor);
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
    }
}
