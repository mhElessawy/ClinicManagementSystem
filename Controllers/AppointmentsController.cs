using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Appointments
        public async Task<IActionResult> Index(bool showDeleted = false, DateTime? filterDate = null, bool showAll = false)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Default to today's date for doctors and assistants
            if (!showAll && !filterDate.HasValue && (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION))
            {
                filterDate = DateTime.Today;
            }

            IQueryable<Appointment> appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Intake);

            // Filter by doctor/assistant (includes doctor group members)
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION)
            {
                if (!doctorId.HasValue)
                {
                    ViewBag.ShowDeleted = showDeleted;
                    ViewBag.FilterDate = filterDate ?? DateTime.Today;
                    ViewBag.ShowAll = showAll;
                    ViewBag.IsDoctorOrAssistant = true;
                    ViewBag.IsAssistant = (userType == SessionHelper.TYPE_ASSISTANT);
                    ViewBag.IsDoctor = (userType == SessionHelper.TYPE_DOCTOR);
                    ViewBag.IsReception = (userType == SessionHelper.TYPE_RECEPTION);
                    return View(new List<Appointment>());
                }
                var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(doctorId.Value);
                appointmentsQuery = appointmentsQuery.Where(a => groupDoctorIds.Contains(a.DoctorId));
            }

            // Filter by date if specified
            if (filterDate.HasValue && !showAll)
            {
                appointmentsQuery = appointmentsQuery.Where(a => a.AppointmentDate == filterDate.Value);
            }

            // Filter deleted
            if (!showDeleted)
            {
                appointmentsQuery = appointmentsQuery.Where(a => !a.IsDeleted);
            }

            var appointments = await appointmentsQuery
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            ViewBag.ShowDeleted = showDeleted;
            ViewBag.FilterDate = filterDate ?? DateTime.Today;
            ViewBag.ShowAll = showAll;
            ViewBag.IsDoctorOrAssistant = (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION);
            ViewBag.IsAssistant = (userType == SessionHelper.TYPE_ASSISTANT);
            ViewBag.IsDoctor = (userType == SessionHelper.TYPE_DOCTOR);
            ViewBag.IsReception = (userType == SessionHelper.TYPE_RECEPTION);
            return View(appointments);
        }

        // GET: Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Intake)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null) return NotFound();

            // Check if diagnosis exists for this appointment
            var existingDiagnosis = await _context.PatientDiagnoses
                .FirstOrDefaultAsync(d => d.AppointmentId == id);
            ViewBag.ExistingDiagnosisId = existingDiagnosis?.Id;

            // Check access
            if (!await CanAccessAppointmentAsync(appointment))
            {
                TempData["Error"] = "You don't have permission to view this appointment";
                return RedirectToAction(nameof(Index));
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        public async Task<IActionResult> Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            await PopulateDropdownsAsync();

            // Set default date to today
            ViewBag.DefaultDate = DateTime.Today.ToString("yyyy-MM-dd");

            return View();
        }

        // POST: Appointments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Appointment appointment)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Set creator information
            appointment.CreatedBy = (int)userId;
            appointment.CreatedByType = userType;
            appointment.CreatedDate = DateTime.Now;

            // Auto-assign doctor if logged in as doctor
            if (userType == SessionHelper.TYPE_DOCTOR && doctorId.HasValue)
            {
                appointment.DoctorId = doctorId.Value;
            }
            else if ((userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION) && doctorId.HasValue)
            {
                appointment.DoctorId = doctorId.Value;
            }

            // Validate appointment date/time
            var appointmentDateTime = appointment.AppointmentDate.Add(appointment.AppointmentTime);
            if (appointmentDateTime < DateTime.Now)
            {
                ModelState.AddModelError("AppointmentDate", "Appointment cannot be in the past");
            }

            // Check for conflicting appointments
            var hasConflict = await _context.Appointments
                .AnyAsync(a => a.DoctorId == appointment.DoctorId
                            && a.AppointmentDate == appointment.AppointmentDate
                            && a.AppointmentTime == appointment.AppointmentTime
                            && a.Status != "Cancelled"
                            && a.Active);

            if (hasConflict)
            {
                ModelState.AddModelError("AppointmentTime", "This time slot is already booked");
            }

            if (ModelState.IsValid)
            {
                _context.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment created successfully";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(appointment.PatientId, appointment.DoctorId);
            ViewBag.DefaultDate = appointment.AppointmentDate.ToString("yyyy-MM-dd");
            return View(appointment);
        }

        // GET: Appointments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            // Check access
            if (!await CanAccessAppointmentAsync(appointment))
            {
                TempData["Error"] = "You don't have permission to edit this appointment";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(appointment.PatientId, appointment.DoctorId);
            return View(appointment);
        }

        // POST: Appointments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Appointment appointment)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != appointment.Id) return NotFound();

            // Check access
            var existingAppointment = await _context.Appointments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (existingAppointment == null || !await CanAccessAppointmentAsync(existingAppointment))
            {
                TempData["Error"] = "You don't have permission to edit this appointment";
                return RedirectToAction(nameof(Index));
            }

            // Validate appointment date/time
            var appointmentDateTime = appointment.AppointmentDate.Add(appointment.AppointmentTime);
            if (appointmentDateTime < DateTime.Now && appointment.Status == "Scheduled")
            {
                ModelState.AddModelError("AppointmentDate", "Appointment cannot be in the past");
            }

            // Check for conflicting appointments (excluding current)
            var hasConflict = await _context.Appointments
                .AnyAsync(a => a.Id != id
                            && a.DoctorId == appointment.DoctorId
                            && a.AppointmentDate == appointment.AppointmentDate
                            && a.AppointmentTime == appointment.AppointmentTime
                            && a.Status != "Cancelled"
                            && a.Active);

            if (hasConflict)
            {
                ModelState.AddModelError("AppointmentTime", "This time slot is already booked");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Preserve creator info
                    appointment.CreatedBy = existingAppointment.CreatedBy;
                    appointment.CreatedByType = existingAppointment.CreatedByType;
                    appointment.CreatedDate = existingAppointment.CreatedDate;

                    _context.Update(appointment);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Appointment updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AppointmentExists(appointment.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(appointment.PatientId, appointment.DoctorId);
            return View(appointment);
        }

        // POST: Appointments/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            // Check access
            if (!await CanAccessAppointmentAsync(appointment))
            {
                TempData["Error"] = "You don't have permission to cancel this appointment";
                return RedirectToAction(nameof(Index));
            }

            appointment.Status = "Cancelled";
            await _context.SaveChangesAsync();
            TempData["Success"] = "Appointment cancelled successfully";

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (appointment == null) return NotFound();

            // Check access
            if (!await CanAccessAppointmentAsync(appointment))
            {
                TempData["Error"] = "You don't have permission to delete this appointment";
                return RedirectToAction(nameof(Index));
            }

            return View(appointment);
        }

        // POST: Appointments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string deletionReason)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(deletionReason))
            {
                TempData["Error"] = "Deletion reason is required";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            // Check access
            if (!await CanAccessAppointmentAsync(appointment))
            {
                TempData["Error"] = "You don't have permission to delete this appointment";
                return RedirectToAction(nameof(Index));
            }

            var userId = SessionHelper.GetUserId(HttpContext.Session);
            var userType = SessionHelper.GetUserType(HttpContext.Session);

            // Soft delete
            appointment.IsDeleted = true;
            appointment.DeletionReason = deletionReason;
            appointment.DeletedBy = userId;
            appointment.DeletedByType = userType;
            appointment.DeletionDate = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Appointment deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        // POST: Appointments/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_DOCTOR && userType != SessionHelper.TYPE_ADMIN)
            {
                TempData["Error"] = "Only doctors can mark appointments as completed";
                return RedirectToAction(nameof(Index));
            }

            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null) return NotFound();

            // Check access
            if (!await CanAccessAppointmentAsync(appointment))
            {
                TempData["Error"] = "You don't have permission to complete this appointment";
                return RedirectToAction(nameof(Index));
            }

            appointment.Status = "Completed";
            await _context.SaveChangesAsync();
            TempData["Success"] = "Appointment marked as completed";

            return RedirectToAction(nameof(Index));
        }

        // GET: Appointments/Calendar
        public async Task<IActionResult> Calendar()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<Appointment> appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Intake);

            // Filter by doctor/assistant (includes doctor group members)
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION)
            {
                if (!doctorId.HasValue)
                {
                    return View(new List<Appointment>());
                }
                var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(doctorId.Value);
                appointmentsQuery = appointmentsQuery.Where(a => groupDoctorIds.Contains(a.DoctorId));
            }

            var appointments = await appointmentsQuery
                .Where(a => a.Status == "Scheduled" && a.Active)
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .ToListAsync();

            return View(appointments);
        }

        private bool AppointmentExists(int id)
        {
            return _context.Appointments.Any(e => e.Id == id);
        }

        private async Task<bool> CanAccessAppointmentAsync(Appointment appointment)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            if (userType == SessionHelper.TYPE_ADMIN)
                return true;

            if ((userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION) && doctorId.HasValue)
            {
                var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(doctorId.Value);
                return groupDoctorIds.Contains(appointment.DoctorId);
            }

            return false;
        }

        private async Task PopulateDropdownsAsync(int? selectedPatient = null, int? selectedDoctor = null)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Patients dropdown
            IQueryable<Patient> patientsQuery = _context.Patients;
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION)
            {
                if (doctorId.HasValue)
                {
                    var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(doctorId.Value);
                    patientsQuery = patientsQuery.Where(p => p.DoctorId.HasValue && groupDoctorIds.Contains(p.DoctorId.Value));
                }
            }

            ViewBag.PatientId = new SelectList(patientsQuery.OrderBy(p => p.PatientName), "Id", "PatientName", selectedPatient);

            // Doctors dropdown
            IQueryable<DoctorInfo> doctorsQuery = _context.DoctorInfos.Where(d => d.Active);
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT || userType == SessionHelper.TYPE_RECEPTION)
            {
                if (doctorId.HasValue)
                {
                    var groupDoctorIds = await _context.GetGroupDoctorIdsAsync(doctorId.Value);
                    doctorsQuery = doctorsQuery.Where(d => groupDoctorIds.Contains(d.Id));
                }
            }

            ViewBag.DoctorId = new SelectList(doctorsQuery.OrderBy(d => d.DoctorName), "Id", "DoctorName", selectedDoctor);
        }
    }
}
