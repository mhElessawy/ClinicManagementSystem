using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class AssistantReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AssistantReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AssistantReports
        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_ASSISTANT)
            {
                TempData["Error"] = "This page is only for assistants";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            // Get doctor info
            var doctor = await _context.DoctorInfos
                .Include(d => d.Specialist)
                .FirstOrDefaultAsync(d => d.Id == doctorId.Value);

            ViewBag.DoctorName = doctor?.DoctorName;
            ViewBag.DoctorSpecialist = doctor?.Specialist?.SpecialistName;

            // Get statistics
            ViewBag.TotalPatients = await _context.Patients
                .CountAsync(p => p.DoctorId == doctorId.Value);

            ViewBag.TotalAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId.Value && !a.IsDeleted);

            ViewBag.ScheduledAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId.Value && a.Status == "Scheduled" && !a.IsDeleted);

            ViewBag.TodayAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId.Value 
                              && a.AppointmentDate.Date == DateTime.Today 
                              && a.Status == "Scheduled" && !a.IsDeleted);

            ViewBag.ThisWeekAppointments = await _context.Appointments
                .CountAsync(a => a.DoctorId == doctorId.Value 
                              && a.AppointmentDate >= DateTime.Today.AddDays(-7) 
                              && a.Status == "Scheduled" && !a.IsDeleted);

            ViewBag.MyAppointmentsCreated = await _context.Appointments
                .CountAsync(a => a.CreatedBy == SessionHelper.GetUserId(HttpContext.Session) 
                              && a.CreatedByType == "Assistant");

            return View();
        }

        // GET: AssistantReports/DoctorPatients
        public async Task<IActionResult> DoctorPatients()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_ASSISTANT)
            {
                TempData["Error"] = "This page is only for assistants";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            var patients = await _context.Patients
                .Where(p => p.DoctorId == doctorId.Value)
                .OrderBy(p => p.PatientName)
                .ToListAsync();

            ViewBag.DoctorName = (await _context.DoctorInfos.FindAsync(doctorId.Value))?.DoctorName;

            return View(patients);
        }

        // GET: AssistantReports/DoctorAppointments
        public async Task<IActionResult> DoctorAppointments(DateTime? fromDate, DateTime? toDate, string status = "All")
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_ASSISTANT)
            {
                TempData["Error"] = "This page is only for assistants";
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

            ViewBag.DoctorName = (await _context.DoctorInfos.FindAsync(doctorId.Value))?.DoctorName;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.SelectedStatus = status;

            return View(appointments);
        }

        // GET: AssistantReports/MyCreatedAppointments
        public async Task<IActionResult> MyCreatedAppointments()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_ASSISTANT)
            {
                TempData["Error"] = "This page is only for assistants";
                return RedirectToAction("Index", "Home");
            }

            var userId = SessionHelper.GetUserId(HttpContext.Session);

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Where(a => a.CreatedBy == userId && a.CreatedByType == "Assistant" && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            return View(appointments);
        }

        // Export Doctor Patients to Excel
        public async Task<IActionResult> ExportDoctorPatients()
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
                var worksheet = workbook.Worksheets.Add("Doctor Patients");
                
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
                        $"Doctor_Patients_{DateTime.Now:yyyyMMdd}.xlsx");
                }
            }
        }
    }
}
