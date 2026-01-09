using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class PatientDiagnosesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientDiagnosesController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm)

        {

            if (!SessionHelper.IsLoggedIn(HttpContext.Session))

                return RedirectToAction("Login", "Account");



            var userType = SessionHelper.GetUserType(HttpContext.Session);

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);



            IQueryable<PatientDiagnosis> diagnosesQuery = _context.PatientDiagnoses

                .Include(p => p.Patient)

                .Include(p => p.Doctor);



            // Filter by doctor

            if (userType == SessionHelper.TYPE_DOCTOR && doctorId.HasValue)

            {

                diagnosesQuery = diagnosesQuery.Where(d => d.DoctorId == doctorId.Value);

            }

            else if (userType == SessionHelper.TYPE_ASSISTANT && doctorId.HasValue)

            {

                diagnosesQuery = diagnosesQuery.Where(d => d.DoctorId == doctorId.Value);

            }



            // Search by patient name

            if (!string.IsNullOrEmpty(searchTerm))

            {

                diagnosesQuery = diagnosesQuery.Where(d => d.Patient.PatientName.Contains(searchTerm));

            }



            // Sort by date - newest first

            diagnosesQuery = diagnosesQuery.OrderByDescending(d => d.DiagnosisDate);



            ViewBag.SearchTerm = searchTerm;

            return View(await diagnosesQuery.ToListAsync());
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
        public async Task<IActionResult> Create(PatientDiagnosis diagnosis, IFormFile? DiagnosisFileUpload)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Auto-assign doctor
            if (userType == SessionHelper.TYPE_DOCTOR && doctorId.HasValue)
            {
                diagnosis.DoctorId = doctorId.Value;
            }

            // Handle file upload to PatientFile folder
            if (DiagnosisFileUpload != null && DiagnosisFileUpload.Length > 0)
            {
                try
                {
                    // Create PatientFile directory if it doesn't exist
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "PatientFile");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(DiagnosisFileUpload.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Save file to disk
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await DiagnosisFileUpload.CopyToAsync(stream);
                    }

                    // Store relative path in database
                    diagnosis.DiagnosisFilePath = $"/PatientFile/{fileName}";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }

            // Remove navigation properties from validation
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(diagnosis);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Diagnosis created successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving diagnosis: {ex.Message}");
                }
            }

            PopulateDropdowns(diagnosis.PatientId, diagnosis.DoctorId);
            return View(diagnosis);
        }

        // GET: PatientDiagnoses/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var diagnosis = await _context.PatientDiagnoses
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (diagnosis == null)
                return NotFound();

            return View(diagnosis);
        }

        // GET: PatientDiagnoses/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var diagnosis = await _context.PatientDiagnoses.FindAsync(id);
            if (diagnosis == null)
                return NotFound();

            PopulateDropdowns(diagnosis.PatientId, diagnosis.DoctorId);
            return View(diagnosis);
        }

        // POST: PatientDiagnoses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PatientDiagnosis diagnosis, IFormFile? DiagnosisFileUpload)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != diagnosis.Id)
                return NotFound();

            // Handle file upload to PatientFile folder
            if (DiagnosisFileUpload != null && DiagnosisFileUpload.Length > 0)
            {
                try
                {
                    // Delete old file if exists
                    var existingDiagnosis = await _context.PatientDiagnoses
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == id);

                    if (!string.IsNullOrEmpty(existingDiagnosis?.DiagnosisFilePath))
                    {
                        var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", existingDiagnosis.DiagnosisFilePath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Create PatientFile directory if it doesn't exist
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "PatientFile");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generate unique filename
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(DiagnosisFileUpload.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    // Save file to disk
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await DiagnosisFileUpload.CopyToAsync(stream);
                    }

                    // Store relative path in database
                    diagnosis.DiagnosisFilePath = $"/PatientFile/{fileName}";
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                }
            }
            else
            {
                // Keep existing file path if no new file uploaded
                var existingDiagnosis = await _context.PatientDiagnoses
                    .AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == id);
                diagnosis.DiagnosisFilePath = existingDiagnosis?.DiagnosisFilePath;
            }

            // Remove navigation properties from validation
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(diagnosis);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Diagnosis updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DiagnosisExists(diagnosis.Id))
                        return NotFound();
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating diagnosis: {ex.Message}");
                }
            }

            PopulateDropdowns(diagnosis.PatientId, diagnosis.DoctorId);
            return View(diagnosis);
        }

        // GET: PatientDiagnoses/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var diagnosis = await _context.PatientDiagnoses
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (diagnosis == null)
                return NotFound();

            return View(diagnosis);
        }

        // POST: PatientDiagnoses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var diagnosis = await _context.PatientDiagnoses.FindAsync(id);
            if (diagnosis != null)
            {
                _context.PatientDiagnoses.Remove(diagnosis);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Diagnosis deleted successfully";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: PatientDiagnoses/GetDiagnosisFile/5
        public async Task<IActionResult> GetDiagnosisFile(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var diagnosis = await _context.PatientDiagnoses.FindAsync(id);
            if (diagnosis == null || string.IsNullOrEmpty(diagnosis.DiagnosisFilePath))
                return NotFound();

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", diagnosis.DiagnosisFilePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var fileName = Path.GetFileName(filePath);

            return File(fileBytes, "application/pdf", fileName);
        }

        private bool DiagnosisExists(int id)
        {
            return _context.PatientDiagnoses.Any(e => e.Id == id);
        }

        private void PopulateDropdowns(int? selectedPatient = null, int? selectedDoctor = null)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<Patient> patientsQuery = _context.Patients;
            IQueryable<DoctorInfo> doctorsQuery = _context.DoctorInfos.Where(d => d.Active);

            if (userType == SessionHelper.TYPE_DOCTOR && doctorId.HasValue)
            {
                patientsQuery = patientsQuery.Where(p => p.DoctorId == doctorId.Value);
                doctorsQuery = doctorsQuery.Where(d => d.Id == doctorId.Value);
            }
            else if (userType == SessionHelper.TYPE_ASSISTANT && doctorId.HasValue)
            {
                patientsQuery = patientsQuery.Where(p => p.DoctorId == doctorId.Value);
                doctorsQuery = doctorsQuery.Where(d => d.Id == doctorId.Value);
            }

            ViewBag.PatientId = new SelectList(patientsQuery, "Id", "PatientName", selectedPatient);
            ViewBag.DoctorId = new SelectList(doctorsQuery, "Id", "DoctorName", selectedDoctor);
        }
    }
}
