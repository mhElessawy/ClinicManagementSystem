using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class DoctorAssistsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorAssistsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var assists = await _context.DoctorAssists
                .Include(d => d.Doctor)
                .ToListAsync();

            return View(assists);
        }

        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            ViewBag.DoctorId = new SelectList(_context.DoctorInfos.Where(d => d.Active), "Id", "DoctorName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorAssist assist)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(assist.LoginPassword))
                {
                    try { assist.LoginPassword = BCrypt.Net.BCrypt.HashPassword(assist.LoginPassword); }
                    catch { }
                }

                _context.Add(assist);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assistant created successfully";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DoctorId = new SelectList(_context.DoctorInfos.Where(d => d.Active), "Id", "DoctorName", assist.DoctorId);
            return View(assist);
        }

        // GET: DoctorAssists/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var assist = await _context.DoctorAssists
                .Include(d => d.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assist == null)
                return NotFound();

            return View(assist);
        }

        // GET: DoctorAssists/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var assist = await _context.DoctorAssists.FindAsync(id);
            if (assist == null)
                return NotFound();

            // Clear password for security
            assist.LoginPassword = "";

            ViewBag.DoctorId = new SelectList(_context.DoctorInfos.Where(d => d.Active), "Id", "DoctorName", assist.DoctorId);
            return View(assist);
        }

        // POST: DoctorAssists/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorAssist assist)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != assist.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingAssist = await _context.DoctorAssists
                        .AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == id);

                    if (existingAssist == null)
                        return NotFound();

                    // Handle password
                    if (!string.IsNullOrEmpty(assist.LoginPassword))
                    {
                        try { assist.LoginPassword = BCrypt.Net.BCrypt.HashPassword(assist.LoginPassword); }
                        catch { }
                    }
                    else
                    {
                        assist.LoginPassword = existingAssist.LoginPassword;
                    }

                    // Preserve LastLoginDate
                    assist.LastLoginDate = existingAssist.LastLoginDate;

                    _context.Update(assist);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Assistant updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorAssistExists(assist.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DoctorId = new SelectList(_context.DoctorInfos.Where(d => d.Active), "Id", "DoctorName", assist.DoctorId);
            return View(assist);
        }

        // GET: DoctorAssists/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var assist = await _context.DoctorAssists
                .Include(d => d.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (assist == null)
                return NotFound();

            return View(assist);
        }

        // POST: DoctorAssists/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var assist = await _context.DoctorAssists.FindAsync(id);
            if (assist != null)
            {
                _context.DoctorAssists.Remove(assist);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assistant deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DoctorAssistExists(int id) => _context.DoctorAssists.Any(e => e.Id == id);
    }
}
