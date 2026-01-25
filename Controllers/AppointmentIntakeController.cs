using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;
using System.Text.Json;

namespace ClinicManagementSystem.Controllers
{
    public class AppointmentIntakeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentIntakeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: AppointmentIntake - List today's appointments pending intake
        public async Task<IActionResult> Index(DateTime? date = null)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);

            // Only assistants can perform intake
            if (userType != SessionHelper.TYPE_ASSISTANT)
            {
                TempData["Error"] = "Only assistants can access patient intake";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
            {
                TempData["Error"] = "No doctor assigned to this assistant";
                return RedirectToAction("Index", "Home");
            }

            var filterDate = date ?? DateTime.Today;

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Intake)
                .Where(a => a.DoctorId == doctorId.Value
                         && a.AppointmentDate == filterDate
                         && a.Status == "Scheduled"
                         && !a.IsDeleted
                         && a.Active)
                .OrderBy(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.FilterDate = filterDate;
            ViewBag.DoctorName = (await _context.DoctorInfos.FindAsync(doctorId.Value))?.DoctorName;

            return View(appointments);
        }

        // GET: AppointmentIntake/Perform/5 - Perform intake for an appointment
        public async Task<IActionResult> Perform(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_ASSISTANT)
            {
                TempData["Error"] = "Only assistants can perform patient intake";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
            {
                TempData["Error"] = "No doctor assigned to this assistant";
                return RedirectToAction("Index", "Home");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.Specialist)
                .Include(a => a.Intake)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            // Check if this appointment belongs to this doctor
            if (appointment.DoctorId != doctorId.Value)
            {
                TempData["Error"] = "You don't have permission to access this appointment";
                return RedirectToAction(nameof(Index));
            }

            // Get specialty-specific questions
            var specialistId = appointment.Doctor?.SpecialistId;
            List<IntakeQuestion> questions = new List<IntakeQuestion>();
            if (specialistId.HasValue)
            {
                questions = await _context.IntakeQuestions
                    .Where(q => q.SpecialistId == specialistId.Value && q.Active)
                    .OrderBy(q => q.DisplayOrder)
                    .ToListAsync();
            }

            // If intake already exists, pre-fill the form
            var intake = appointment.Intake ?? new AppointmentIntake
            {
                AppointmentId = appointment.Id,
                ArrivalTime = DateTime.Now
            };

            ViewBag.Appointment = appointment;
            ViewBag.Questions = questions;

            return View(intake);
        }

        // POST: AppointmentIntake/Perform/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Perform(int id, AppointmentIntake intake, Dictionary<string, string>? questionResponses)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_ASSISTANT)
            {
                TempData["Error"] = "Only assistants can perform patient intake";
                return RedirectToAction("Index", "Home");
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
            {
                TempData["Error"] = "No doctor assigned to this assistant";
                return RedirectToAction("Index", "Home");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                    .ThenInclude(d => d!.Specialist)
                .Include(a => a.Intake)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            if (appointment.DoctorId != doctorId.Value)
            {
                TempData["Error"] = "You don't have permission to access this appointment";
                return RedirectToAction(nameof(Index));
            }

            // Store question responses as JSON
            if (questionResponses != null && questionResponses.Any())
            {
                intake.SpecialtyQuestionsJson = JsonSerializer.Serialize(questionResponses);
            }

            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var userName = SessionHelper.GetFullName(HttpContext.Session);

            intake.AppointmentId = id;
            intake.PerformedBy = userId ?? 0;
            intake.PerformedByName = userName;
            intake.PatientArrived = true;
            intake.ArrivalTime ??= DateTime.Now;

            // Check if intake already exists
            var existingIntake = await _context.AppointmentIntakes
                .FirstOrDefaultAsync(ai => ai.AppointmentId == id);

            if (existingIntake != null)
            {
                // Update existing
                existingIntake.BloodPressure = intake.BloodPressure;
                existingIntake.HeartRate = intake.HeartRate;
                existingIntake.Temperature = intake.Temperature;
                existingIntake.Weight = intake.Weight;
                existingIntake.Height = intake.Height;
                existingIntake.ChiefComplaint = intake.ChiefComplaint;
                existingIntake.CurrentSymptoms = intake.CurrentSymptoms;
                existingIntake.SymptomDuration = intake.SymptomDuration;
                existingIntake.PainLevel = intake.PainLevel;
                existingIntake.CurrentMedications = intake.CurrentMedications;
                existingIntake.Allergies = intake.Allergies;
                existingIntake.PreviousConditions = intake.PreviousConditions;
                existingIntake.SpecialtyQuestionsJson = intake.SpecialtyQuestionsJson;
                existingIntake.AdditionalNotes = intake.AdditionalNotes;
                existingIntake.ReadyForDoctor = intake.ReadyForDoctor;
                existingIntake.LastUpdated = DateTime.Now;

                _context.Update(existingIntake);
            }
            else
            {
                // Create new
                intake.IntakeDate = DateTime.Now;
                _context.AppointmentIntakes.Add(intake);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Patient intake saved successfully";

            return RedirectToAction(nameof(Index));
        }

        // GET: AppointmentIntake/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            var intake = await _context.AppointmentIntakes
                .Include(ai => ai.Appointment)
                    .ThenInclude(a => a!.Patient)
                .Include(ai => ai.Appointment)
                    .ThenInclude(a => a!.Doctor)
                        .ThenInclude(d => d!.Specialist)
                .FirstOrDefaultAsync(ai => ai.Id == id);

            if (intake == null) return NotFound();

            // Check access
            if (userType != SessionHelper.TYPE_ADMIN)
            {
                if (!doctorId.HasValue || intake.Appointment?.DoctorId != doctorId.Value)
                {
                    TempData["Error"] = "You don't have permission to view this intake";
                    return RedirectToAction(nameof(Index));
                }
            }

            // Get specialty questions for display
            var specialistId = intake.Appointment?.Doctor?.SpecialistId;
            if (specialistId.HasValue)
            {
                var questions = await _context.IntakeQuestions
                    .Where(q => q.SpecialistId == specialistId.Value && q.Active)
                    .OrderBy(q => q.DisplayOrder)
                    .ToListAsync();
                ViewBag.Questions = questions;
            }

            // Parse question responses
            if (!string.IsNullOrEmpty(intake.SpecialtyQuestionsJson))
            {
                try
                {
                    ViewBag.QuestionResponses = JsonSerializer.Deserialize<Dictionary<string, string>>(intake.SpecialtyQuestionsJson);
                }
                catch
                {
                    ViewBag.QuestionResponses = new Dictionary<string, string>();
                }
            }

            return View(intake);
        }

        // POST: AppointmentIntake/MarkReady/5 - Mark patient as ready for doctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkReady(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_ASSISTANT)
            {
                TempData["Error"] = "Only assistants can mark patients as ready";
                return RedirectToAction(nameof(Index));
            }

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            var intake = await _context.AppointmentIntakes
                .Include(ai => ai.Appointment)
                .FirstOrDefaultAsync(ai => ai.Id == id);

            if (intake == null) return NotFound();

            if (intake.Appointment?.DoctorId != doctorId)
            {
                TempData["Error"] = "You don't have permission to modify this intake";
                return RedirectToAction(nameof(Index));
            }

            intake.ReadyForDoctor = true;
            intake.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Patient marked as ready for doctor";
            return RedirectToAction(nameof(Index));
        }

        // GET: AppointmentIntake/ViewByAppointment/5 - View intake for a specific appointment
        public async Task<IActionResult> ViewByAppointment(int? appointmentId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (appointmentId == null) return NotFound();

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            var intake = await _context.AppointmentIntakes
                .Include(ai => ai.Appointment)
                    .ThenInclude(a => a!.Patient)
                .Include(ai => ai.Appointment)
                    .ThenInclude(a => a!.Doctor)
                        .ThenInclude(d => d!.Specialist)
                .FirstOrDefaultAsync(ai => ai.AppointmentId == appointmentId);

            if (intake == null)
            {
                TempData["Info"] = "No intake recorded for this appointment";
                return RedirectToAction("Details", "Appointments", new { id = appointmentId });
            }

            return RedirectToAction(nameof(Details), new { id = intake.Id });
        }
    }
}
