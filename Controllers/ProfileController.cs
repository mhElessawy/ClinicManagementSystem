using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;
using ClinicManagementSystem.Services;

namespace ClinicManagementSystem.Controllers
{
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileProcessingService _fileProcessingService;

        public ProfileController(ApplicationDbContext context, IFileProcessingService fileProcessingService)
        {
            _context = context;
            _fileProcessingService = fileProcessingService;
        }

        // GET: Profile
        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            ViewBag.UserType = userType;

            if (userType == SessionHelper.TYPE_ADMIN)
            {
                var user = await _context.UserInfos
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);
                return View("AdminProfile", user);
            }
            else if (userType == SessionHelper.TYPE_DOCTOR)
            {
                var doctor = await _context.DoctorInfos
                    .Include(d => d.Specialist)
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.Id == doctorId);
                return View("DoctorProfile", doctor);
            }
            else if (userType == SessionHelper.TYPE_ASSISTANT)
            {
                var assistant = await _context.DoctorAssists
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.Id == userId);
                return View("AssistantProfile", assistant);
            }

            return NotFound();
        }

        // GET: Profile/EditAdmin
        public async Task<IActionResult> EditAdmin()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_ADMIN)
                return Forbid();

            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var user = await _context.UserInfos.FindAsync(userId);

            if (user == null) return NotFound();

            return View(user);
        }

        // POST: Profile/EditAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdmin(UserInfo user)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userId = SessionHelper.GetUserId(HttpContext.Session);
            if (user.Id != userId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.UserInfos.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                    user.UserPassword = existingUser?.UserPassword ?? "";
                    user.RoleId = existingUser?.RoleId;

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    
                    // Update session
                    SessionHelper.SetUserSession(
                        HttpContext.Session,
                        user.Id,
                        user.UserName,
                        SessionHelper.TYPE_ADMIN,
                        user.UserFullName ?? user.UserName,
                        null,
                        user.RoleId
                    );

                    TempData["Success"] = "Profile updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["Error"] = "Error updating profile";
                }
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        // GET: Profile/EditDoctor
        public async Task<IActionResult> EditDoctor()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
                return Forbid();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var doctor = await _context.DoctorInfos.FindAsync(doctorId);

            if (doctor == null) return NotFound();

            ViewBag.SpecialistId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                _context.Specialists, "Id", "SpecialistName", doctor.SpecialistId);

            return View(doctor);
        }

        // POST: Profile/EditDoctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(DoctorInfo doctor, IFormFile? DoctorPictureFile)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (doctor.Id != doctorId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDoctor = await _context.DoctorInfos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == doctorId);

                    // Handle image with resizing
                    if (DoctorPictureFile != null && DoctorPictureFile.Length > 0)
                    {
                        // Resize image to max 400x400 pixels with 75% quality for doctor pictures
                        doctor.DoctorPicture = await _fileProcessingService.ResizeImageAsync(
                            DoctorPictureFile, maxWidth: 400, maxHeight: 400, quality: 75);
                    }
                    else
                    {
                        doctor.DoctorPicture = existingDoctor?.DoctorPicture;
                    }

                    // Preserve login credentials
                    doctor.LoginUsername = existingDoctor?.LoginUsername;
                    doctor.LoginPassword = existingDoctor?.LoginPassword;
                    doctor.CanLogin = existingDoctor?.CanLogin ?? false;
                    doctor.LastLoginDate = existingDoctor?.LastLoginDate;
                    doctor.RegDate = existingDoctor?.RegDate;
                    doctor.UserId = existingDoctor?.UserId;

                    _context.Update(doctor);
                    await _context.SaveChangesAsync();

                    // Update session
                    SessionHelper.SetUserSession(
                        HttpContext.Session,
                        doctor.Id,
                        doctor.LoginUsername ?? "",
                        SessionHelper.TYPE_DOCTOR,
                        doctor.DoctorName,
                        doctor.Id,
                        null
                    );

                    TempData["Success"] = "Profile updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["Error"] = "Error updating profile";
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.SpecialistId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                _context.Specialists, "Id", "SpecialistName", doctor.SpecialistId);
            return View(doctor);
        }

        // GET: Profile/ChangePassword
        public IActionResult ChangePassword()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "All fields are required";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New password and confirm password do not match";
                return View();
            }

            if (newPassword.Length < 6)
            {
                ViewBag.Error = "Password must be at least 6 characters";
                return View();
            }

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            try
            {
                if (userType == SessionHelper.TYPE_ADMIN)
                {
                    var user = await _context.UserInfos.FindAsync(userId);
                    if (user == null)
                    {
                        ViewBag.Error = "User not found";
                        return View();
                    }

                    // Verify current password
                    bool isValid = false;
                    try { isValid = BCrypt.Net.BCrypt.Verify(currentPassword, user.UserPassword); }
                    catch { isValid = user.UserPassword == currentPassword; }

                    if (!isValid)
                    {
                        ViewBag.Error = "Current password is incorrect";
                        return View();
                    }

                    // Update password
                    try { user.UserPassword = BCrypt.Net.BCrypt.HashPassword(newPassword); }
                    catch { user.UserPassword = newPassword; }

                    await _context.SaveChangesAsync();
                }
                else if (userType == SessionHelper.TYPE_DOCTOR)
                {
                    var doctor = await _context.DoctorInfos.FindAsync(doctorId);
                    if (doctor == null)
                    {
                        ViewBag.Error = "Doctor not found";
                        return View();
                    }

                    // Verify current password
                    bool isValid = false;
                    try { isValid = BCrypt.Net.BCrypt.Verify(currentPassword, doctor.LoginPassword ?? ""); }
                    catch { isValid = doctor.LoginPassword == currentPassword; }

                    if (!isValid)
                    {
                        ViewBag.Error = "Current password is incorrect";
                        return View();
                    }

                    // Update password
                    try { doctor.LoginPassword = BCrypt.Net.BCrypt.HashPassword(newPassword); }
                    catch { doctor.LoginPassword = newPassword; }

                    await _context.SaveChangesAsync();
                }
                else if (userType == SessionHelper.TYPE_ASSISTANT)
                {
                    var assistant = await _context.DoctorAssists.FindAsync(userId);
                    if (assistant == null)
                    {
                        ViewBag.Error = "Assistant not found";
                        return View();
                    }

                    // Verify current password
                    bool isValid = false;
                    try { isValid = BCrypt.Net.BCrypt.Verify(currentPassword, assistant.LoginPassword ?? ""); }
                    catch { isValid = assistant.LoginPassword == currentPassword; }

                    if (!isValid)
                    {
                        ViewBag.Error = "Current password is incorrect";
                        return View();
                    }

                    // Update password
                    try { assistant.LoginPassword = BCrypt.Net.BCrypt.HashPassword(newPassword); }
                    catch { assistant.LoginPassword = newPassword; }

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Password changed successfully";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error changing password: " + ex.Message;
                return View();
            }
        }
    }
}
