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

        private bool DoctorAssistExists(int id) => _context.DoctorAssists.Any(e => e.Id == id);
    }
}
