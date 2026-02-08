using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class DoctorGroupsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorGroupsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session) || !SessionHelper.IsAdmin(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var groups = await _context.DoctorGroups
                .Include(g => g.Members)
                    .ThenInclude(m => m.Doctor)
                .OrderBy(g => g.GroupName)
                .ToListAsync();

            return View(groups);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session) || !SessionHelper.IsAdmin(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var group = await _context.DoctorGroups
                .Include(g => g.Members)
                    .ThenInclude(m => m.Doctor)
                        .ThenInclude(d => d.Specialist)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            return View(group);
        }

        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session) || !SessionHelper.IsAdmin(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            ViewBag.Doctors = _context.DoctorInfos
                .Where(d => d.Active)
                .OrderBy(d => d.DoctorName)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorGroup group, int[] SelectedDoctorIds)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session) || !SessionHelper.IsAdmin(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                group.CreatedDate = DateTime.Now;
                _context.Add(group);
                await _context.SaveChangesAsync();

                // Add selected doctors as members
                if (SelectedDoctorIds != null && SelectedDoctorIds.Length > 0)
                {
                    foreach (var doctorId in SelectedDoctorIds)
                    {
                        _context.DoctorGroupMembers.Add(new DoctorGroupMember
                        {
                            DoctorGroupId = group.Id,
                            DoctorId = doctorId,
                            AddedDate = DateTime.Now
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "Doctor group created successfully";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Doctors = _context.DoctorInfos
                .Where(d => d.Active)
                .OrderBy(d => d.DoctorName)
                .ToList();

            return View(group);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session) || !SessionHelper.IsAdmin(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var group = await _context.DoctorGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            ViewBag.Doctors = _context.DoctorInfos
                .Where(d => d.Active)
                .OrderBy(d => d.DoctorName)
                .ToList();

            ViewBag.SelectedDoctorIds = group.Members.Select(m => m.DoctorId).ToList();

            return View(group);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorGroup group, int[] SelectedDoctorIds)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session) || !SessionHelper.IsAdmin(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != group.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingGroup = await _context.DoctorGroups
                        .Include(g => g.Members)
                        .FirstOrDefaultAsync(g => g.Id == id);

                    if (existingGroup == null) return NotFound();

                    existingGroup.GroupName = group.GroupName;
                    existingGroup.Description = group.Description;
                    existingGroup.Active = group.Active;

                    // Remove old members
                    _context.DoctorGroupMembers.RemoveRange(existingGroup.Members);

                    // Add new members
                    if (SelectedDoctorIds != null && SelectedDoctorIds.Length > 0)
                    {
                        foreach (var doctorId in SelectedDoctorIds)
                        {
                            _context.DoctorGroupMembers.Add(new DoctorGroupMember
                            {
                                DoctorGroupId = existingGroup.Id,
                                DoctorId = doctorId,
                                AddedDate = DateTime.Now
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Doctor group updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DoctorGroups.Any(g => g.Id == id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Doctors = _context.DoctorInfos
                .Where(d => d.Active)
                .OrderBy(d => d.DoctorName)
                .ToList();

            ViewBag.SelectedDoctorIds = SelectedDoctorIds?.ToList() ?? new List<int>();

            return View(group);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session) || !SessionHelper.IsAdmin(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var group = await _context.DoctorGroups
                .Include(g => g.Members)
                    .ThenInclude(m => m.Doctor)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (group == null) return NotFound();

            return View(group);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session) || !SessionHelper.IsAdmin(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var group = await _context.DoctorGroups.FindAsync(id);
            if (group != null)
            {
                _context.DoctorGroups.Remove(group);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Doctor group deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
