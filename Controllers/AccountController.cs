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
