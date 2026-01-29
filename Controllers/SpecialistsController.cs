using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class SpecialistsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SpecialistsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? departmentId, string searchName)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var specialistsQuery = _context.Specialists
                .Include(s => s.Department)
                .AsQueryable();

            // Filter by Department
            if (departmentId.HasValue && departmentId.Value > 0)
            {
                specialistsQuery = specialistsQuery.Where(s => s.DepartmentId == departmentId.Value);
            }

            // Search by Name
            if (!string.IsNullOrEmpty(searchName))
            {
                specialistsQuery = specialistsQuery.Where(s => s.SpecialistName.Contains(searchName));
            }

            var specialists = await specialistsQuery.ToListAsync();

            // Populate departments dropdown for filter
            ViewBag.Departments = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "Id", "DepartmentName", departmentId);
            ViewBag.SearchName = searchName;
            ViewBag.SelectedDepartment = departmentId;

            return View(specialists);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var specialist = await _context.Specialists
                .Include(s => s.Department)
                .Include(s => s.Doctors)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (specialist == null) return NotFound();

            return View(specialist);
        }

        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            ViewBag.DepartmentId = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "Id", "DepartmentName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DepartmentId,SpecialistName,Description")] Specialist specialist)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                _context.Add(specialist);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Specialist created successfully";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DepartmentId = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "Id", "DepartmentName", specialist.DepartmentId);
            return View(specialist);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var specialist = await _context.Specialists.FindAsync(id);
            if (specialist == null) return NotFound();

            ViewBag.DepartmentId = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "Id", "DepartmentName", specialist.DepartmentId);
            return View(specialist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,  Specialist specialist)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != specialist.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(specialist);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Specialist updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SpecialistExists(specialist.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.DepartmentId = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "Id", "DepartmentName", specialist.DepartmentId);
            return View(specialist);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var specialist = await _context.Specialists
                .Include(s => s.Department)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (specialist == null) return NotFound();

            return View(specialist);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var specialist = await _context.Specialists.FindAsync(id);
            if (specialist != null)
            {
                _context.Specialists.Remove(specialist);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Specialist deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SpecialistExists(int id)
        {
            return _context.Specialists.Any(e => e.Id == id);
        }
    }
}
