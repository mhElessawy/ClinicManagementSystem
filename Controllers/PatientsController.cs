using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Patients
        public async Task<IActionResult> Index(string searchName, string searchCivilID, string searchTel)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<Patient> patientsQuery = _context.Patients.Include(p => p.Doctor);

            // Filter patients based on user type (includes doctor group members)
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION)
            {
                if (doctorId.HasValue)
                {
                    var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(doctorId.Value);
                    patientsQuery = patientsQuery.Where(p => p.DoctorId.HasValue && groupDoctorIds.Contains(p.DoctorId.Value));
                }
                else
                {
                    return View(new List<Patient>());
                }
            }

            // Apply search filters
            if (!string.IsNullOrEmpty(searchName))
            {
                patientsQuery = patientsQuery.Where(p => p.PatientName.Contains(searchName));
            }

            if (!string.IsNullOrEmpty(searchCivilID))
            {
                patientsQuery = patientsQuery.Where(p => p.PatientCivilID != null && p.PatientCivilID.Contains(searchCivilID));
            }

            if (!string.IsNullOrEmpty(searchTel))
            {
                patientsQuery = patientsQuery.Where(p =>
                    (p.PatientTel1 != null && p.PatientTel1.Contains(searchTel)) ||
                    (p.PatientTel2 != null && p.PatientTel2.Contains(searchTel)));
            }

            // Store search values in ViewBag for display
            ViewBag.SearchName = searchName;
            ViewBag.SearchCivilID = searchCivilID;
            ViewBag.SearchTel = searchTel;

            return View(await patientsQuery.ToListAsync());
        }

        // GET: Patients/Create
        public async Task<IActionResult> Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            await PopulateDoctorDropdownAsync();
            return View();
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patient patient)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Auto-assign doctor only for doctors; assistants/receptionists can choose from their group
            if (userType == SessionHelper.TYPE_DOCTOR && doctorId.HasValue)
            {
                patient.DoctorId = doctorId.Value;
            }
            else if ((userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION) && doctorId.HasValue)
            {
                // Validate selected doctor is in the user's group
                var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(doctorId.Value);
                if (!patient.DoctorId.HasValue || !groupDoctorIds.Contains(patient.DoctorId.Value))
                {
                    ModelState.AddModelError("DoctorId", "ÌÃ» «Œ Ì«— ÿ»Ì» „‰ «·„Ã„Ê⁄…");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(patient);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Patient added successfully";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDoctorDropdownAsync(patient.DoctorId);
            return View(patient);
        }

        private async Task PopulateDoctorDropdownAsync(int? selectedValue = null)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<DoctorInfo> doctorsQuery = _context.DoctorInfos.Where(d => d.Active);

            if ((userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION) && doctorId.HasValue)
            {
                var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(doctorId.Value);
                doctorsQuery = doctorsQuery.Where(d => groupDoctorIds.Contains(d.Id));
            }

            ViewBag.DoctorId = new SelectList(doctorsQuery, "Id", "DoctorName", selectedValue);
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null) return NotFound();

            // Check access
            if (!await CanAccessPatientAsync(patient))
            {
                TempData["Error"] = "You don't have permission to view this patient";
                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }

        // GET: Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            // Check access
            if (!await CanAccessPatientAsync(patient))
            {
                TempData["Error"] = "You don't have permission to edit this patient";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDoctorDropdownAsync(patient.DoctorId);
            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patient patient)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != patient.Id) return NotFound();

            // Check access for existing patient
            var existingPatient = await _context.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existingPatient == null || !await CanAccessPatientAsync(existingPatient))
            {
                TempData["Error"] = "You don't have permission to edit this patient";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Patient updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDoctorDropdownAsync(patient.DoctorId);
            return View(patient);
        }

        // GET: Patients/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null) return NotFound();

            // Check access
            if (!await CanAccessPatientAsync(patient))
            {
                TempData["Error"] = "You don't have permission to delete this patient";
                return RedirectToAction(nameof(Index));
            }

            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var patient = await _context.Patients
                .Include(p => p.PatientDiagnoses)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            // Check access
            if (!await CanAccessPatientAsync(patient))
            {
                TempData["Error"] = "You don't have permission to delete this patient";
                return RedirectToAction(nameof(Index));
            }

            // Delete all diagnosis files associated with this patient
            foreach (var diagnosis in patient.PatientDiagnoses)
            {
                if (!string.IsNullOrEmpty(diagnosis.DiagnosisFilePath))
                {
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", diagnosis.DiagnosisFilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }
            }

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Patient deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }

        private async Task<bool> CanAccessPatientAsync(Patient patient)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            if (userType == SessionHelper.TYPE_ADMIN)
                return true;

            if ((userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION) && doctorId.HasValue)
            {
                var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(doctorId.Value);
                return patient.DoctorId.HasValue && groupDoctorIds.Contains(patient.DoctorId.Value);
            }

            return false;
        }
    }
}