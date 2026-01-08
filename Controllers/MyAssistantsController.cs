using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class MyAssistantsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MyAssistantsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MyAssistants
        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can manage assistants";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var assistants = await _context.DoctorAssists
                .Include(a => a.Doctor)
                .Where(a => a.DoctorId == doctorId)
                .ToListAsync();

            return View(assistants);
        }

        // GET: MyAssistants/Create
        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can add assistants";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: MyAssistants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorAssist assistant)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can add assistants";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Auto-assign to current doctor
            assistant.DoctorId = doctorId.Value;

            // Check if username already exists (if provided)
            if (!string.IsNullOrEmpty(assistant.LoginUsername))
            {
                var existingAssist = await _context.DoctorAssists
                    .AnyAsync(a => a.LoginUsername == assistant.LoginUsername);

                if (existingAssist)
                {
                    ModelState.AddModelError("LoginUsername", "This username is already taken");
                    return View(assistant);
                }
            }

            if (ModelState.IsValid)
            {
                // Hash password if provided
                if (!string.IsNullOrEmpty(assistant.LoginPassword))
                {
                    try { assistant.LoginPassword = BCrypt.Net.BCrypt.HashPassword(assistant.LoginPassword); }
                    catch { }
                }

                _context.Add(assistant);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assistant added successfully";
                return RedirectToAction(nameof(Index));
            }

            return View(assistant);
        }

        // GET: MyAssistants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can edit assistants";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var assistant = await _context.DoctorAssists
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctorId);

            if (assistant == null)
            {
                TempData["Error"] = "Assistant not found or does not belong to you";
                return RedirectToAction(nameof(Index));
            }

            // Clear password for security
            assistant.LoginPassword = "";

            return View(assistant);
        }

        // POST: MyAssistants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorAssist assistant)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can edit assistants";
                return RedirectToAction("Index", "Home");
            }

            if (id != assistant.Id) return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            
            // Verify assistant belongs to this doctor
            var existingAssist = await _context.DoctorAssists
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctorId);

            if (existingAssist == null)
            {
                TempData["Error"] = "Assistant not found or does not belong to you";
                return RedirectToAction(nameof(Index));
            }

            // Ensure DoctorId doesn't change
            assistant.DoctorId = doctorId.Value;

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle password
                    if (!string.IsNullOrEmpty(assistant.LoginPassword))
                    {
                        try { assistant.LoginPassword = BCrypt.Net.BCrypt.HashPassword(assistant.LoginPassword); }
                        catch { }
                    }
                    else
                    {
                        assistant.LoginPassword = existingAssist.LoginPassword;
                    }

                    // Preserve LastLoginDate
                    assistant.LastLoginDate = existingAssist.LastLoginDate;

                    _context.Update(assistant);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Assistant updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AssistantExists(assistant.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(assistant);
        }

        // GET: MyAssistants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can delete assistants";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var assistant = await _context.DoctorAssists
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctorId);

            if (assistant == null)
            {
                TempData["Error"] = "Assistant not found or does not belong to you";
                return RedirectToAction(nameof(Index));
            }

            return View(assistant);
        }

        // POST: MyAssistants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can delete assistants";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var assistant = await _context.DoctorAssists
                .FirstOrDefaultAsync(a => a.Id == id && a.DoctorId == doctorId);

            if (assistant != null)
            {
                _context.DoctorAssists.Remove(assistant);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assistant deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AssistantExists(int id)
        {
            return _context.DoctorAssists.Any(e => e.Id == id);
        }
    }
}
