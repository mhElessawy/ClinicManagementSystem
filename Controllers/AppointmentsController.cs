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
        public async Task<IActionResult> Index(bool showDeleted = false)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            IQueryable<Appointment> appointmentsQuery = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Doctor)
                .Include(a => a.Intake);

            // Filter by doctor/assistant - MUST have doctorId to see appointments
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (!doctorId.HasValue)
                {
                    // Doctor/Assistant without valid DoctorId - return empty list
                    return View(new List<Appointment>());
                }
                appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == doctorId.Value);
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
            ViewBag.IsDoctorOrAssistant = (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT);
            ViewBag.IsAssistant = (userType == SessionHelper.TYPE_ASSISTANT);
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

            // Check access
            if (!CanAccessAppointment(appointment))
            {
                TempData["Error"] = "You don't have permission to view this appointment";
                return RedirectToAction(nameof(Index));
            }

            return View(appointment);
        }

        // GET: Appointments/Create
        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            PopulateDropdowns();
            
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
            else if (userType == SessionHelper.TYPE_ASSISTANT && doctorId.HasValue)
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

            PopulateDropdowns(appointment.PatientId, appointment.DoctorId);
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
            if (!CanAccessAppointment(appointment))
            {
                TempData["Error"] = "You don't have permission to edit this appointment";
                return RedirectToAction(nameof(Index));
            }

            PopulateDropdowns(appointment.PatientId, appointment.DoctorId);
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
            if (existingAppointment == null || !CanAccessAppointment(existingAppointment))
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

            PopulateDropdowns(appointment.PatientId, appointment.DoctorId);
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
            if (!CanAccessAppointment(appointment))
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
            if (!CanAccessAppointment(appointment))
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
            if (!CanAccessAppointment(appointment))
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
            if (!CanAccessAppointment(appointment))
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

            // Filter by doctor/assistant - MUST have doctorId to see appointments
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (!doctorId.HasValue)
                {
                    return View(new List<Appointment>());
                }
                appointmentsQuery = appointmentsQuery.Where(a => a.DoctorId == doctorId.Value);
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

        private bool CanAccessAppointment(Appointment appointment)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            if (userType == SessionHelper.TYPE_ADMIN)
                return true;

            if ((userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT) && doctorId.HasValue)
                return appointment.DoctorId == doctorId.Value;

            return false;
        }

        private void PopulateDropdowns(int? selectedPatient = null, int? selectedDoctor = null)
        {
            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Patients dropdown
            IQueryable<Patient> patientsQuery = _context.Patients;
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (doctorId.HasValue)
                    patientsQuery = patientsQuery.Where(p => p.DoctorId == doctorId.Value);
            }

            ViewBag.PatientId = new SelectList(patientsQuery.OrderBy(p => p.PatientName), "Id", "PatientName", selectedPatient);

            // Doctors dropdown
            IQueryable<DoctorInfo> doctorsQuery = _context.DoctorInfos.Where(d => d.Active);
            if (userType == SessionHelper.TYPE_DOCTOR || userType == SessionHelper.TYPE_ASSISTANT)
            {
                if (doctorId.HasValue)
                    doctorsQuery = doctorsQuery.Where(d => d.Id == doctorId.Value);
            }

            ViewBag.DoctorId = new SelectList(doctorsQuery.OrderBy(d => d.DoctorName), "Id", "DoctorName", selectedDoctor);
        }
    }
}
