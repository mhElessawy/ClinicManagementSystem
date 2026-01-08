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

        public async Task<IActionResult> Index()
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

            if (DiagnosisFileUpload != null && DiagnosisFileUpload.Length > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await DiagnosisFileUpload.CopyToAsync(ms);
                    diagnosis.DiagnosisFile = ms.ToArray();
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(diagnosis);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Diagnosis created successfully";
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(diagnosis.PatientId, diagnosis.DoctorId);
            return View(diagnosis);
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
