using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class UserInfosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UserInfosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var users = await _context.UserInfos
                .Include(u => u.Role)
                .ToListAsync();

            return View(users);
        }

        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            ViewBag.RoleId = new SelectList(_context.Roles.Where(r => r.Active), "Id", "RoleName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserInfo user)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (await _context.UserInfos.AnyAsync(u => u.UserName == user.UserName))
            {
                ModelState.AddModelError("UserName", "Username already exists");
                ViewBag.RoleId = new SelectList(_context.Roles.Where(r => r.Active), "Id", "RoleName", user.RoleId);
                return View(user);
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(user.UserPassword))
                {
                    try { user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword); }
                    catch { }
                }

                _context.Add(user);
                await _context.SaveChangesAsync();
                TempData["Success"] = "User created successfully";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.RoleId = new SelectList(_context.Roles.Where(r => r.Active), "Id", "RoleName", user.RoleId);
            return View(user);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var user = await _context.UserInfos.FindAsync(id);
            if (user == null) return NotFound();

            user.UserPassword = ""; // Clear for security
            ViewBag.RoleId = new SelectList(_context.Roles.Where(r => r.Active), "Id", "RoleName", user.RoleId);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserInfo user)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != user.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.UserInfos.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

                    if (!string.IsNullOrEmpty(user.UserPassword))
                    {
                        try { user.UserPassword = BCrypt.Net.BCrypt.HashPassword(user.UserPassword); }
                        catch { }
                    }
                    else
                    {
                        user.UserPassword = existingUser?.UserPassword ?? "";
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "User updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.RoleId = new SelectList(_context.Roles.Where(r => r.Active), "Id", "RoleName", user.RoleId);
            return View(user);
        }

        private bool UserExists(int id) => _context.UserInfos.Any(e => e.Id == id);
    }
}
