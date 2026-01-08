using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public IActionResult Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            return View();
        }

        // GET: Reports/Doctors
        public async Task<IActionResult> DoctorsReport()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var doctors = await _context.DoctorInfos
                .Include(d => d.Specialist)
                    .ThenInclude(s => s.Department)
                .Include(d => d.User)
                .OrderBy(d => d.DoctorName)
                .ToListAsync();

            return View(doctors);
        }

        // GET: Reports/Patients
        public async Task<IActionResult> PatientsReport()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<Patient> patientsQuery = _context.Patients.Include(p => p.Doctor);

            // Filter by doctor if not admin
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (doctorId.HasValue)
                    patientsQuery = patientsQuery.Where(p => p.DoctorId == doctorId.Value);
            }

            var patients = await patientsQuery.OrderBy(p => p.PatientName).ToListAsync();

            return View(patients);
        }

        // GET: Reports/Diagnoses
        public async Task<IActionResult> DiagnosesReport(DateTime? fromDate, DateTime? toDate, int? doctorId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var currentDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<PatientDiagnosis> diagnosesQuery = _context.PatientDiagnoses
                .Include(d => d.Patient)
                .Include(d => d.Doctor);

            // Filter by doctor type
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (currentDoctorId.HasValue)
                    diagnosesQuery = diagnosesQuery.Where(d => d.DoctorId == currentDoctorId.Value);
            }

            // Filter by date range
            if (fromDate.HasValue)
                diagnosesQuery = diagnosesQuery.Where(d => d.DiagnosisDate >= fromDate.Value);

            if (toDate.HasValue)
                diagnosesQuery = diagnosesQuery.Where(d => d.DiagnosisDate <= toDate.Value);

            // Filter by doctor (for admin only)
            if (userType == SessionHelper.TYPE_ADMIN && doctorId.HasValue)
                diagnosesQuery = diagnosesQuery.Where(d => d.DoctorId == doctorId.Value);

            var diagnoses = await diagnosesQuery
                .OrderByDescending(d => d.DiagnosisDate)
                .ToListAsync();

            // For filters
            ViewBag.Doctors = await _context.DoctorInfos
                .Where(d => d.Active)
                .OrderBy(d => d.DoctorName)
                .ToListAsync();

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedDoctorId = doctorId;

            return View(diagnoses);
        }

        // GET: Reports/Assistants
        public async Task<IActionResult> AssistantsReport()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var assistants = await _context.DoctorAssists
                .Include(a => a.Doctor)
                .OrderBy(a => a.AssistName)
                .ToListAsync();

            return View(assistants);
        }

        // GET: Reports/Statistics
        public async Task<IActionResult> StatisticsReport()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Total counts
            ViewBag.TotalDoctors = await _context.DoctorInfos.CountAsync(d => d.Active);
            ViewBag.TotalDepartments = await _context.Departments.CountAsync();
            ViewBag.TotalSpecialists = await _context.Specialists.CountAsync();
            ViewBag.TotalAssistants = await _context.DoctorAssists.CountAsync(a => a.Active);

            if (userType == SessionHelper.TYPE_ADMIN)
            {
                ViewBag.TotalPatients = await _context.Patients.CountAsync();
                ViewBag.TotalDiagnoses = await _context.PatientDiagnoses.CountAsync();
                ViewBag.TodayDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DiagnosisDate.Date == DateTime.Today);
                ViewBag.ThisWeekDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DiagnosisDate >= DateTime.Today.AddDays(-7));
                ViewBag.ThisMonthDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DiagnosisDate.Month == DateTime.Today.Month 
                                  && d.DiagnosisDate.Year == DateTime.Today.Year);
            }
            else if (doctorId.HasValue)
            {
                ViewBag.TotalPatients = await _context.Patients.CountAsync(p => p.DoctorId == doctorId.Value);
                ViewBag.TotalDiagnoses = await _context.PatientDiagnoses.CountAsync(d => d.DoctorId == doctorId.Value);
                ViewBag.TodayDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value && d.DiagnosisDate.Date == DateTime.Today);
                ViewBag.ThisWeekDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value && d.DiagnosisDate >= DateTime.Today.AddDays(-7));
                ViewBag.ThisMonthDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value 
                                  && d.DiagnosisDate.Month == DateTime.Today.Month 
                                  && d.DiagnosisDate.Year == DateTime.Today.Year);
            }

            // Patients by Doctor
            var patientsByDoctor = await _context.Patients
                .Include(p => p.Doctor)
                .GroupBy(p => p.Doctor.DoctorName)
                .Select(g => new { DoctorName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            ViewBag.PatientsByDoctor = patientsByDoctor;

            return View();
        }

        // Export Doctors to Excel
        public async Task<IActionResult> ExportDoctors()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var doctors = await _context.DoctorInfos
                .Include(d => d.Specialist)
                .Include(d => d.User)
                .OrderBy(d => d.DoctorName)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Doctors");
                
                // Headers
                worksheet.Cell(1, 1).Value = "Doctor Name";
                worksheet.Cell(1, 2).Value = "Title";
                worksheet.Cell(1, 3).Value = "Specialist";
                worksheet.Cell(1, 4).Value = "Civil ID";
                worksheet.Cell(1, 5).Value = "Phone 1";
                worksheet.Cell(1, 6).Value = "Phone 2";
                worksheet.Cell(1, 7).Value = "Gender";
                worksheet.Cell(1, 8).Value = "Active";
                worksheet.Cell(1, 9).Value = "Registration Date";

                // Style headers
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

                // Data
                int row = 2;
                foreach (var doctor in doctors)
                {
                    worksheet.Cell(row, 1).Value = doctor.DoctorName;
                    worksheet.Cell(row, 2).Value = doctor.DoctorTitle ?? "";
                    worksheet.Cell(row, 3).Value = doctor.Specialist?.SpecialistName ?? "";
                    worksheet.Cell(row, 4).Value = doctor.DoctorCivilId ?? "";
                    worksheet.Cell(row, 5).Value = doctor.DoctorTel1 ?? "";
                    worksheet.Cell(row, 6).Value = doctor.DoctorTel2 ?? "";
                    worksheet.Cell(row, 7).Value = doctor.Gender ?? "";
                    worksheet.Cell(row, 8).Value = doctor.Active ? "Yes" : "No";
                    worksheet.Cell(row, 9).Value = doctor.RegDate?.ToString("yyyy-MM-dd") ?? "";
                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"Doctors_Report_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        // Export Patients to Excel
        public async Task<IActionResult> ExportPatients()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<Patient> patientsQuery = _context.Patients.Include(p => p.Doctor);

            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (doctorId.HasValue)
                    patientsQuery = patientsQuery.Where(p => p.DoctorId == doctorId.Value);
            }

            var patients = await patientsQuery.OrderBy(p => p.PatientName).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Patients");
                
                // Headers
                worksheet.Cell(1, 1).Value = "Patient Name";
                worksheet.Cell(1, 2).Value = "Civil ID";
                worksheet.Cell(1, 3).Value = "Phone 1";
                worksheet.Cell(1, 4).Value = "Phone 2";
                worksheet.Cell(1, 5).Value = "Address";
                worksheet.Cell(1, 6).Value = "Doctor";

                // Style headers
                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGreen;

                // Data
                int row = 2;
                foreach (var patient in patients)
                {
                    worksheet.Cell(row, 1).Value = patient.PatientName;
                    worksheet.Cell(row, 2).Value = patient.PatientCivilID ?? "";
                    worksheet.Cell(row, 3).Value = patient.PatientTel1 ?? "";
                    worksheet.Cell(row, 4).Value = patient.PatientTel2 ?? "";
                    worksheet.Cell(row, 5).Value = patient.PatientAddress ?? "";
                    worksheet.Cell(row, 6).Value = patient.Doctor?.DoctorName ?? "";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"Patients_Report_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        // Export Diagnoses to Excel
        public async Task<IActionResult> ExportDiagnoses(DateTime? fromDate, DateTime? toDate, int? doctorId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var currentDoctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<PatientDiagnosis> diagnosesQuery = _context.PatientDiagnoses
                .Include(d => d.Patient)
                .Include(d => d.Doctor);

            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (currentDoctorId.HasValue)
                    diagnosesQuery = diagnosesQuery.Where(d => d.DoctorId == currentDoctorId.Value);
            }

            if (fromDate.HasValue)
                diagnosesQuery = diagnosesQuery.Where(d => d.DiagnosisDate >= fromDate.Value);

            if (toDate.HasValue)
                diagnosesQuery = diagnosesQuery.Where(d => d.DiagnosisDate <= toDate.Value);

            if (userType == SessionHelper.TYPE_ADMIN && doctorId.HasValue)
                diagnosesQuery = diagnosesQuery.Where(d => d.DoctorId == doctorId.Value);

            var diagnoses = await diagnosesQuery
                .OrderByDescending(d => d.DiagnosisDate)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Diagnoses");
                
                // Headers
                worksheet.Cell(1, 1).Value = "Date";
                worksheet.Cell(1, 2).Value = "Patient Name";
                worksheet.Cell(1, 3).Value = "Doctor Name";
                worksheet.Cell(1, 4).Value = "Diagnosis Details";
                worksheet.Cell(1, 5).Value = "Status";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightYellow;

                int row = 2;
                foreach (var diagnosis in diagnoses)
                {
                    worksheet.Cell(row, 1).Value = diagnosis.DiagnosisDate.ToString("yyyy-MM-dd");
                    worksheet.Cell(row, 2).Value = diagnosis.Patient?.PatientName ?? "";
                    worksheet.Cell(row, 3).Value = diagnosis.Doctor?.DoctorName ?? "";
                    worksheet.Cell(row, 4).Value = diagnosis.DiagnosisDetails ?? "";
                    worksheet.Cell(row, 5).Value = diagnosis.Active ? "Active" : "Inactive";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"Diagnoses_Report_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}
