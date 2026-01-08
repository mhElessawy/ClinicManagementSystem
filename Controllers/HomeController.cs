using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Calculate stats based on user type
            ViewBag.TotalDoctors = await _context.DoctorInfos.CountAsync(d => d.Active);
            
            if (userType == SessionHelper.TYPE_ADMIN)
            {
                ViewBag.TotalPatients = await _context.Patients.CountAsync();
                ViewBag.TotalDiagnoses = await _context.PatientDiagnoses.CountAsync();
                ViewBag.TodayDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DiagnosisDate.Date == DateTime.Today);
            }
            else if (userType == SessionHelper.TYPE_DOCTOR && doctorId.HasValue)
            {
                ViewBag.TotalPatients = await _context.Patients
                    .CountAsync(p => p.DoctorId == doctorId.Value);
                ViewBag.TotalDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value);
                ViewBag.TodayDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value && d.DiagnosisDate.Date == DateTime.Today);

                // Get today's and tomorrow's appointments
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                ViewBag.TodayAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId.Value
                             && a.AppointmentDate.Date == today
                             && !a.IsDeleted
                             && a.Status == "Scheduled")
                    .OrderBy(a => a.AppointmentTime)
                    .ToListAsync();

                ViewBag.TomorrowAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId.Value
                             && a.AppointmentDate.Date == tomorrow
                             && !a.IsDeleted
                             && a.Status == "Scheduled")
                    .OrderBy(a => a.AppointmentTime)
                    .ToListAsync();

                // Get doctor's subscription information
                var doctor = await _context.DoctorInfos
                    .Include(d => d.Subscriptions)
                    .FirstOrDefaultAsync(d => d.Id == doctorId.Value);

                if (doctor != null)
                {
                    var currentSubscription = doctor.Subscriptions?
                        .Where(s => s.IsActive && s.StartDate <= today && s.EndDate >= today)
                        .OrderByDescending(s => s.EndDate)
                        .FirstOrDefault();

                    ViewBag.CurrentSubscription = currentSubscription;
                }
            }
            else if (userType == SessionHelper.TYPE_ASSISTANT && doctorId.HasValue)
            {
                ViewBag.TotalPatients = await _context.Patients
                    .CountAsync(p => p.DoctorId == doctorId.Value);
                ViewBag.TotalDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value);
                ViewBag.TodayDiagnoses = await _context.PatientDiagnoses
                    .CountAsync(d => d.DoctorId == doctorId.Value && d.DiagnosisDate.Date == DateTime.Today);
                
                // Get today's and tomorrow's appointments
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                
                ViewBag.TodayAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId.Value 
                             && a.AppointmentDate.Date == today 
                             && !a.IsDeleted
                             && a.Status == "Scheduled")
                    .OrderBy(a => a.AppointmentTime)
                    .ToListAsync();
                
                ViewBag.TomorrowAppointments = await _context.Appointments
                    .Include(a => a.Patient)
                    .Where(a => a.DoctorId == doctorId.Value 
                             && a.AppointmentDate.Date == tomorrow 
                             && !a.IsDeleted
                             && a.Status == "Scheduled")
                    .OrderBy(a => a.AppointmentTime)
                    .ToListAsync();
            }

            ViewBag.UserName = SessionHelper.GetFullName(HttpContext.Session);
            ViewBag.UserType = userType;

            return View();
        }
    }
}
