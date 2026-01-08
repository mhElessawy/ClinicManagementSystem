using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class DoctorReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DoctorReports
        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "This page is only for doctors";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            // Get statistics
            ViewBag.TotalPatients = await _context.Patients
                .CountAsync(p => p.DoctorId == doctorId.Value);

            ViewBag.TotalAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId.Value && !a.IsDeleted);

            ViewBag.ScheduledAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId.Value && a.Status == "Scheduled" && !a.IsDeleted);

            ViewBag.CompletedAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId.Value && a.Status == "Completed");

            ViewBag.TodayAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId.Value 
                              && a.AppointmentDate.Date == DateTime.Today 
                              && a.Status == "Scheduled" && !a.IsDeleted);

            ViewBag.ThisWeekAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId.Value 
                              && a.AppointmentDate >= DateTime.Today.AddDays(-7) 
                              && a.Status == "Scheduled" && !a.IsDeleted);

            ViewBag.TotalDiagnoses = await _context.PatientDiagnoses
                .CountAsync(d => d.DoctorId == doctorId.Value);

            ViewBag.TotalAssistants = await _context.DoctorAssists
                .CountAsync(a => a.DoctorId == doctorId.Value && a.Active);

            return View();
        }

        // GET: DoctorReports/MyPatients
        public async Task<IActionResult> MyPatients()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "This page is only for doctors";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            var patients = await _context.Patients
                .Where(p => p.DoctorId == doctorId.Value)
                .OrderBy(p => p.PatientName)
                .ToListAsync();

            return View(patients);
        }

        // GET: DoctorReports/MyAppointments
        public async Task<IActionResult> MyAppointments(DateTime? fromDate, DateTime? toDate, string status = "All")
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "This page is only for doctors";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            IQueryable<Appointment> appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId.Value && !a.IsDeleted);

            if (fromDate.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate >= fromDate.Value);

            if (toDate.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate <= toDate.Value);

            if (status != "All")
                appointmentsQuery = appointmentsQuery.Where(a => a.Status == status);

            var appointments = await appointmentsQuery
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedStatus = status;

            return View(appointments);
        }

        // GET: DoctorReports/MyDiagnoses
        public async Task<IActionResult> MyDiagnoses(DateTime? fromDate, DateTime? toDate)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "This page is only for doctors";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            IQueryable<PatientDiagnosis> diagnosesQuery = _context.PatientDiagnoses
                .Include(d => d.Patient)
                .Where(d => d.DoctorId == doctorId.Value);

            if (fromDate.HasValue)
                diagnosesQuery = diagnosesQuery.Where(d => d.DiagnosisDate >= fromDate.Value);

            if (toDate.HasValue)
                diagnosesQuery = diagnosesQuery.Where(d => d.DiagnosisDate <= toDate.Value);

            var diagnoses = await diagnosesQuery
                .OrderByDescending(d => d.DiagnosisDate)
                .ToListAsync();

            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            return View(diagnoses);
        }

        // GET: DoctorReports/MyAssistants
        public async Task<IActionResult> MyAssistantsReport()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "This page is only for doctors";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            var assistants = await _context.DoctorAssists
                .Where(a => a.DoctorId == doctorId.Value)
                .OrderBy(a => a.AssistName)
                .ToListAsync();

            return View(assistants);
        }

        // Export My Patients to Excel
        public async Task<IActionResult> ExportMyPatients()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            var patients = await _context.Patients
                .Where(p => p.DoctorId == doctorId.Value)
                .OrderBy(p => p.PatientName)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("My Patients");
                
                worksheet.Cell(1, 1).Value = "Patient Name";
                worksheet.Cell(1, 2).Value = "Civil ID";
                worksheet.Cell(1, 3).Value = "Phone 1";
                worksheet.Cell(1, 4).Value = "Phone 2";
                worksheet.Cell(1, 5).Value = "Address";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightBlue;

                int row = 2;
                foreach (var patient in patients)
                {
                    worksheet.Cell(row, 1).Value = patient.PatientName;
                    worksheet.Cell(row, 2).Value = patient.PatientCivilID ?? "";
                    worksheet.Cell(row, 3).Value = patient.PatientTel1 ?? "";
                    worksheet.Cell(row, 4).Value = patient.PatientTel2 ?? "";
                    worksheet.Cell(row, 5).Value = patient.PatientAddress ?? "";
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"My_Patients_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }

        // Export My Appointments to Excel
        public async Task<IActionResult> ExportMyAppointments(DateTime? fromDate, DateTime? toDate, string status = "All")
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            IQueryable<Appointment> appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId.Value && !a.IsDeleted);

            if (fromDate.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate >= fromDate.Value);

            if (toDate.HasValue)
                appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate <= toDate.Value);

            if (status != "All")
                appointmentsQuery = appointmentsQuery.Where(a => a.Status == status);

            var appointments = await appointmentsQuery.OrderBy(a => a.AppointmentDate).ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("My Appointments");
                
                worksheet.Cell(1, 1).Value = "Date";
                worksheet.Cell(1, 2).Value = "Time";
                worksheet.Cell(1, 3).Value = "Patient Name";
                worksheet.Cell(1, 4).Value = "Reason";
                worksheet.Cell(1, 5).Value = "Status";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGreen;

                int row = 2;
                foreach (var appt in appointments)
                {
                    worksheet.Cell(row, 1).Value = appt.AppointmentDate.ToString("yyyy-MM-dd");
                    worksheet.Cell(row, 2).Value = appt.AppointmentTime.ToString(@"hh\:mm");
                    worksheet.Cell(row, 3).Value = appt.Patient?.PatientName ?? "";
                    worksheet.Cell(row, 4).Value = appt.Reason;
                    worksheet.Cell(row, 5).Value = appt.Status;
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    return File(content, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        $"My_Appointments_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}
