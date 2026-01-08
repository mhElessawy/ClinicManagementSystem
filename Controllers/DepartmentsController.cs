using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            return View(await _context.Departments.Include(d => d.Specialists).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.Specialists)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (department == null) return NotFound();

            return View(department);
        }

        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DepartmentName")] Department department)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Department created successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();

            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DepartmentName")] Department department)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != department.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Department updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var department = await _context.Departments
                .Include(d => d.Specialists)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (department == null) return NotFound();

            return View(department);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Department deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.Id == id);
        }
    }
}
