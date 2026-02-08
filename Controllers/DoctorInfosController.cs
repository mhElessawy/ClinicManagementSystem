using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;
using ClinicManagementSystem.Services;

namespace ClinicManagementSystem.Controllers
{
    public class DoctorInfosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileProcessingService _fileProcessingService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DoctorInfosController(ApplicationDbContext context, IFileProcessingService fileProcessingService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _fileProcessingService = fileProcessingService;
            _webHostEnvironment = webHostEnvironment;
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
        public async Task<IActionResult> Create(DoctorInfo doctor, IFormFile? DoctorPictureFile, int? DepartmentId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                // Handle image upload with resizing and save to folder
                if (DoctorPictureFile != null && DoctorPictureFile.Length > 0)
                {
                    doctor.DoctorPicture = await _fileProcessingService.SaveDoctorPictureAsync(
                        DoctorPictureFile, _webHostEnvironment.WebRootPath);
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

            PopulateDropdowns(doctor.SpecialistId, doctor.UserId, DepartmentId);
            return View(doctor);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var doctor = await _context.DoctorInfos
                .Include(d => d.Specialist)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null) return NotFound();

            // Clear password for security
            ViewBag.CurrentHasPassword = !string.IsNullOrEmpty(doctor.LoginPassword);
            doctor.LoginPassword = "";

            // Get selected department from specialist
            var selectedDepartment = doctor.Specialist?.DepartmentId;
            PopulateDropdowns(doctor.SpecialistId, doctor.UserId, selectedDepartment);
            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorInfo doctor, IFormFile? DoctorPictureFile, int? DepartmentId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != doctor.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDoctor = await _context.DoctorInfos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

                    // Handle image - save to folder
                    if (DoctorPictureFile != null && DoctorPictureFile.Length > 0)
                    {
                        // Delete old picture if exists
                        _fileProcessingService.DeleteDoctorPicture(existingDoctor?.DoctorPicture, _webHostEnvironment.WebRootPath);

                        // Save new picture
                        doctor.DoctorPicture = await _fileProcessingService.SaveDoctorPictureAsync(
                            DoctorPictureFile, _webHostEnvironment.WebRootPath);
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

            PopulateDropdowns(doctor.SpecialistId, doctor.UserId, DepartmentId);
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
                // Delete the doctor's picture file if exists
                _fileProcessingService.DeleteDoctorPicture(doctor.DoctorPicture, _webHostEnvironment.WebRootPath);

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

        private void PopulateDropdowns(int? selectedSpecialist = null, int? selectedUser = null, int? selectedDepartment = null)
        {
            ViewBag.DepartmentId = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "Id", "DepartmentName", selectedDepartment);
            ViewBag.SpecialistId = new SelectList(_context.Specialists.OrderBy(d => d.SpecialistName), "Id", "SpecialistName", selectedSpecialist);
            ViewBag.UserId = new SelectList(_context.UserInfos.Where(u => u.Active), "Id", "UserFullName", selectedUser);

            // Pass specialists data as JSON for JavaScript filtering
            var specialistsData = _context.Specialists.OrderBy(d => d.SpecialistName).Select(s => new {
                id = s.Id,
                name = s.SpecialistName,
                departmentId = s.DepartmentId
            }).ToList();
            ViewBag.SpecialistsJson = System.Text.Json.JsonSerializer.Serialize(specialistsData);
        }

        // API endpoint to get specialists by department
        [HttpGet]
        public IActionResult GetSpecialistsByDepartment(int departmentId)
        {
            var specialists = _context.Specialists
                .Where(s => s.DepartmentId == departmentId)
                .Select(s => new { id = s.Id, name = s.SpecialistName })
                .ToList();

            return Json(specialists);
        }
    }
}
