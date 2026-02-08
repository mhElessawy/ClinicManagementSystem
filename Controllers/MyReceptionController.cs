using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class MyReceptionController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MyReceptionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MyReception
        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can manage receptionists";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var receptionists = await _context.DoctorReceptions
                .Include(r => r.Doctor)
                .Where(r => r.DoctorId == doctorId)
                .ToListAsync();

            return View(receptionists);
        }

        // GET: MyReception/Create
        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can add receptionists";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: MyReception/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorReception reception)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can add receptionists";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Auto-assign to current doctor
            reception.DoctorId = doctorId!.Value;

            // Check if username already exists (if provided)
            if (!string.IsNullOrEmpty(reception.LoginUsername))
            {
                var existingReception = await _context.DoctorReceptions
                    .AnyAsync(r => r.LoginUsername == reception.LoginUsername);

                if (existingReception)
                {
                    ModelState.AddModelError("LoginUsername", "This username is already taken");
                    return View(reception);
                }
            }

            if (ModelState.IsValid)
            {
                // Hash password if provided
                if (!string.IsNullOrEmpty(reception.LoginPassword))
                {
                    try { reception.LoginPassword = BCrypt.Net.BCrypt.HashPassword(reception.LoginPassword); }
                    catch { }
                }

                _context.Add(reception);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Receptionist added successfully";
                return RedirectToAction(nameof(Index));
            }

            return View(reception);
        }

        // GET: MyReception/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can edit receptionists";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var reception = await _context.DoctorReceptions
                .FirstOrDefaultAsync(r => r.Id == id && r.DoctorId == doctorId);

            if (reception == null)
            {
                TempData["Error"] = "Receptionist not found or does not belong to you";
                return RedirectToAction(nameof(Index));
            }

            // Clear password for security
            reception.LoginPassword = "";

            return View(reception);
        }

        // POST: MyReception/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorReception reception)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can edit receptionists";
                return RedirectToAction("Index", "Home");
            }

            if (id != reception.Id) return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Verify reception belongs to this doctor
            var existingReception = await _context.DoctorReceptions
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && r.DoctorId == doctorId);

            if (existingReception == null)
            {
                TempData["Error"] = "Receptionist not found or does not belong to you";
                return RedirectToAction(nameof(Index));
            }

            // Ensure DoctorId doesn't change
            reception.DoctorId = doctorId!.Value;

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle password
                    if (!string.IsNullOrEmpty(reception.LoginPassword))
                    {
                        try { reception.LoginPassword = BCrypt.Net.BCrypt.HashPassword(reception.LoginPassword); }
                        catch { }
                    }
                    else
                    {
                        reception.LoginPassword = existingReception.LoginPassword;
                    }

                    // Preserve LastLoginDate
                    reception.LastLoginDate = existingReception.LastLoginDate;

                    _context.Update(reception);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Receptionist updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReceptionExists(reception.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(reception);
        }

        // GET: MyReception/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can delete receptionists";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var reception = await _context.DoctorReceptions
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Id == id && r.DoctorId == doctorId);

            if (reception == null)
            {
                TempData["Error"] = "Receptionist not found or does not belong to you";
                return RedirectToAction(nameof(Index));
            }

            return View(reception);
        }

        // POST: MyReception/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (SessionHelper.GetUserType(HttpContext.Session) != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can delete receptionists";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var reception = await _context.DoctorReceptions
                .FirstOrDefaultAsync(r => r.Id == id && r.DoctorId == doctorId);

            if (reception != null)
            {
                _context.DoctorReceptions.Remove(reception);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Receptionist deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ReceptionExists(int id)
        {
            return _context.DoctorReceptions.Any(e => e.Id == id);
        }
    }
}