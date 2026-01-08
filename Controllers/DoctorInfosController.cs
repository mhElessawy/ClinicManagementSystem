using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class DoctorInfosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorInfosController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var doctors = await _context.DoctorInfos
                .Include(d => d.Specialist)
                    .ThenInclude(s => s.Department)
                .Include(d => d.User)
                .Include(d => d.Subscriptions)
                .ToListAsync();

            return View(doctors);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var doctor = await _context.DoctorInfos
                .Include(d => d.Specialist)
                .Include(d => d.User)
                .Include(d => d.Patients)
                .Include(d => d.DoctorAssists)
                .Include(d => d.Subscriptions)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null) return NotFound();

            return View(doctor);
        }

        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            PopulateDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorInfo doctor, IFormFile? DoctorPictureFile)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                // Handle image upload
                if (DoctorPictureFile != null && DoctorPictureFile.Length > 0)
                {
                    using (var ms = new MemoryStream())
                    {
                        await DoctorPictureFile.CopyToAsync(ms);
                        doctor.DoctorPicture = ms.ToArray();
                    }
                }

                // Hash password if provided
                if (!string.IsNullOrEmpty(doctor.LoginPassword))
                {
                    try
                    {
                        doctor.LoginPassword = BCrypt.Net.BCrypt.HashPassword(doctor.LoginPassword);
                    }
                    catch
                    {
                        // Keep plain text if BCrypt fails
                    }
                }

                doctor.RegDate = DateTime.Now;
                _context.Add(doctor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Doctor created successfully";
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(doctor.SpecialistId, doctor.UserId);
            return View(doctor);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var doctor = await _context.DoctorInfos.FindAsync(id);
            if (doctor == null) return NotFound();

            // Clear password for security
            ViewBag.CurrentHasPassword = !string.IsNullOrEmpty(doctor.LoginPassword);
            doctor.LoginPassword = "";

            PopulateDropdowns(doctor.SpecialistId, doctor.UserId);
            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorInfo doctor, IFormFile? DoctorPictureFile)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != doctor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDoctor = await _context.DoctorInfos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

                    // Handle image
                    if (DoctorPictureFile != null && DoctorPictureFile.Length > 0)
                    {
                        using (var ms = new MemoryStream())
                        {
                            await DoctorPictureFile.CopyToAsync(ms);
                            doctor.DoctorPicture = ms.ToArray();
                        }
                    }
                    else
                    {
                        doctor.DoctorPicture = existingDoctor?.DoctorPicture;
                    }

                    // Handle password
                    if (!string.IsNullOrEmpty(doctor.LoginPassword))
                    {
                        try
                        {
                            doctor.LoginPassword = BCrypt.Net.BCrypt.HashPassword(doctor.LoginPassword);
                        }
                        catch
                        {
                            // Keep plain text if BCrypt fails
                        }
                    }
                    else
                    {
                        doctor.LoginPassword = existingDoctor?.LoginPassword;
                    }

                    doctor.RegDate = existingDoctor?.RegDate ?? DateTime.Now;

                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Doctor updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DoctorExists(doctor.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(doctor.SpecialistId, doctor.UserId);
            return View(doctor);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var doctor = await _context.DoctorInfos
                .Include(d => d.Specialist)
                .Include(d => d.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null) return NotFound();

            return View(doctor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var doctor = await _context.DoctorInfos.FindAsync(id);
            if (doctor != null)
            {
                _context.DoctorInfos.Remove(doctor);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Doctor deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool DoctorExists(int id)
        {
            return _context.DoctorInfos.Any(e => e.Id == id);
        }

        private void PopulateDropdowns(int? selectedSpecialist = null, int? selectedUser = null)
        {
            ViewBag.SpecialistId = new SelectList(_context.Specialists, "Id", "SpecialistName", selectedSpecialist);
            ViewBag.UserId = new SelectList(_context.UserInfos.Where(u => u.Active), "Id", "UserFullName", selectedUser);
        }
    }
}
