using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;
using ClinicManagementSystem.Services;

namespace ClinicManagementSystem.Controllers
{
    public class ClinicManagerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IFileProcessingService _fileProcessingService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ClinicManagerController(ApplicationDbContext context, IFileProcessingService fileProcessingService, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _fileProcessingService = fileProcessingService;
            _webHostEnvironment = webHostEnvironment;
        }

        private bool IsClinicManager() => SessionHelper.IsClinicManager(HttpContext.Session);
        private int? GetGroupId() => SessionHelper.GetGroupId(HttpContext.Session);

        // GET: ClinicManager (Dashboard)
        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var groupId = GetGroupId();
            List<int> doctorIds = new();

            if (groupId.HasValue)
            {
                doctorIds = await _context.DoctorGroupMembers
                    .Where(m => m.DoctorGroupId == groupId.Value)
                    .Select(m => m.DoctorId)
                    .ToListAsync();
            }

            ViewBag.TotalDoctors = doctorIds.Count;
            ViewBag.TotalAssistants = await _context.DoctorAssists
                .CountAsync(a => doctorIds.Contains(a.DoctorId));
            ViewBag.TotalReceptionists = await _context.DoctorReceptions
                .CountAsync(r => doctorIds.Contains(r.DoctorId));
            ViewBag.TotalIncome = await _context.DoctorSubscriptions
                .Where(s => doctorIds.Contains(s.DoctorId))
                .SumAsync(s => (decimal?)(s.CostMoney ?? 0)) ?? 0;

            var groupName = groupId.HasValue
                ? await _context.DoctorGroups
                    .Where(g => g.Id == groupId.Value)
                    .Select(g => g.GroupName)
                    .FirstOrDefaultAsync()
                : null;
            ViewBag.GroupName = groupName ?? "No Group Assigned";

            return View();
        }

        // GET: ClinicManager/Doctors
        public async Task<IActionResult> Doctors()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var groupId = GetGroupId();
            List<DoctorInfo> doctors = new();

            if (groupId.HasValue)
            {
                var doctorIds = await _context.DoctorGroupMembers
                    .Where(m => m.DoctorGroupId == groupId.Value)
                    .Select(m => m.DoctorId)
                    .ToListAsync();

                doctors = await _context.DoctorInfos
                    .Include(d => d.Specialist)
                        .ThenInclude(s => s!.Department)
                    .Include(d => d.Subscriptions)
                    .Where(d => doctorIds.Contains(d.Id))
                    .ToListAsync();
            }

            return View(doctors);
        }

        // GET: ClinicManager/AddDoctor
        public IActionResult AddDoctor()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            if (!GetGroupId().HasValue)
            {
                TempData["Error"] = "No group assigned. Please contact the system administrator.";
                return RedirectToAction(nameof(Doctors));
            }

            PopulateDoctorDropdowns();
            return View();
        }

        // POST: ClinicManager/AddDoctor
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(DoctorInfo doctor, IFormFile? DoctorPictureFile, int? DepartmentId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var groupId = GetGroupId();
            if (!groupId.HasValue)
            {
                TempData["Error"] = "No group assigned.";
                return RedirectToAction(nameof(Doctors));
            }

            if (ModelState.IsValid)
            {
                if (DoctorPictureFile != null && DoctorPictureFile.Length > 0)
                {
                    doctor.DoctorPicture = await _fileProcessingService.SaveDoctorPictureAsync(
                        DoctorPictureFile, _webHostEnvironment.WebRootPath);
                }

                if (!string.IsNullOrEmpty(doctor.LoginPassword))
                {
                    try { doctor.LoginPassword = BCrypt.Net.BCrypt.HashPassword(doctor.LoginPassword); }
                    catch { }
                }

                doctor.RegDate = DateTime.Now;
                _context.Add(doctor);
                await _context.SaveChangesAsync();

                // Auto-add to the clinic manager's group
                _context.DoctorGroupMembers.Add(new DoctorGroupMember
                {
                    DoctorGroupId = groupId.Value,
                    DoctorId = doctor.Id,
                    AddedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();

                TempData["Success"] = "Doctor added successfully and assigned to your group";
                return RedirectToAction(nameof(Doctors));
            }

            PopulateDoctorDropdowns(doctor.SpecialistId, DepartmentId);
            return View(doctor);
        }

        // GET: ClinicManager/EditDoctor/5
        public async Task<IActionResult> EditDoctor(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorIds = await GetGroupDoctorIdsAsync();
            var doctor = await _context.DoctorInfos
                .Include(d => d.Specialist)
                .FirstOrDefaultAsync(d => d.Id == id && doctorIds.Contains(d.Id));

            if (doctor == null)
            {
                TempData["Error"] = "Doctor not found or not in your group";
                return RedirectToAction(nameof(Doctors));
            }

            ViewBag.CurrentHasPassword = !string.IsNullOrEmpty(doctor.LoginPassword);
            doctor.LoginPassword = "";
            PopulateDoctorDropdowns(doctor.SpecialistId, doctor.Specialist?.DepartmentId);
            return View(doctor);
        }

        // POST: ClinicManager/EditDoctor/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(int id, DoctorInfo doctor, IFormFile? DoctorPictureFile, int? DepartmentId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            if (id != doctor.Id) return NotFound();

            var doctorIds = await GetGroupDoctorIdsAsync();
            if (!doctorIds.Contains(id))
            {
                TempData["Error"] = "Doctor not found or not in your group";
                return RedirectToAction(nameof(Doctors));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingDoctor = await _context.DoctorInfos.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);

                    if (DoctorPictureFile != null && DoctorPictureFile.Length > 0)
                    {
                        _fileProcessingService.DeleteDoctorPicture(existingDoctor?.DoctorPicture, _webHostEnvironment.WebRootPath);
                        doctor.DoctorPicture = await _fileProcessingService.SaveDoctorPictureAsync(
                            DoctorPictureFile, _webHostEnvironment.WebRootPath);
                    }
                    else
                    {
                        doctor.DoctorPicture = existingDoctor?.DoctorPicture;
                    }

                    if (!string.IsNullOrEmpty(doctor.LoginPassword))
                    {
                        try { doctor.LoginPassword = BCrypt.Net.BCrypt.HashPassword(doctor.LoginPassword); }
                        catch { }
                    }
                    else
                    {
                        doctor.LoginPassword = existingDoctor?.LoginPassword;
                    }

                    doctor.RegDate = existingDoctor?.RegDate ?? DateTime.Now;
                    _context.Update(doctor);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Doctor updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DoctorInfos.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Doctors));
            }

            PopulateDoctorDropdowns(doctor.SpecialistId, DepartmentId);
            return View(doctor);
        }

        // GET: ClinicManager/Income
        public async Task<IActionResult> Income()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var doctorIds = await GetGroupDoctorIdsAsync();

            var subscriptions = await _context.DoctorSubscriptions
                .Include(s => s.Doctor)
                .Where(s => doctorIds.Contains(s.DoctorId))
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            ViewBag.TotalIncome = subscriptions.Sum(s => s.CostMoney ?? 0);
            ViewBag.ActiveSubscriptions = subscriptions.Count(s => s.IsCurrentlyValid);

            return View(subscriptions);
        }

        // GET: ClinicManager/Assistants
        public async Task<IActionResult> Assistants()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var doctorIds = await GetGroupDoctorIdsAsync();

            var assistants = await _context.DoctorAssists
                .Include(a => a.Doctor)
                .Where(a => doctorIds.Contains(a.DoctorId))
                .OrderBy(a => a.Doctor!.DoctorName)
                .ToListAsync();

            return View(assistants);
        }

        // GET: ClinicManager/AddAssistant
        public async Task<IActionResult> AddAssistant()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            await PopulateGroupDoctors();
            return View();
        }

        // POST: ClinicManager/AddAssistant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAssistant(DoctorAssist assistant)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var doctorIds = await GetGroupDoctorIdsAsync();
            if (!doctorIds.Contains(assistant.DoctorId))
            {
                TempData["Error"] = "Selected doctor is not in your group";
                await PopulateGroupDoctors();
                return View(assistant);
            }

            if (!string.IsNullOrEmpty(assistant.LoginUsername))
            {
                if (await _context.DoctorAssists.AnyAsync(a => a.LoginUsername == assistant.LoginUsername))
                {
                    ModelState.AddModelError("LoginUsername", "This username is already taken");
                    await PopulateGroupDoctors(assistant.DoctorId);
                    return View(assistant);
                }
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(assistant.LoginPassword))
                {
                    try { assistant.LoginPassword = BCrypt.Net.BCrypt.HashPassword(assistant.LoginPassword); }
                    catch { }
                }

                _context.Add(assistant);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assistant added successfully";
                return RedirectToAction(nameof(Assistants));
            }

            await PopulateGroupDoctors(assistant.DoctorId);
            return View(assistant);
        }

        // GET: ClinicManager/EditAssistant/5
        public async Task<IActionResult> EditAssistant(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorIds = await GetGroupDoctorIdsAsync();
            var assistant = await _context.DoctorAssists
                .FirstOrDefaultAsync(a => a.Id == id && doctorIds.Contains(a.DoctorId));

            if (assistant == null)
            {
                TempData["Error"] = "Assistant not found or not in your group";
                return RedirectToAction(nameof(Assistants));
            }

            assistant.LoginPassword = "";
            await PopulateGroupDoctors(assistant.DoctorId);
            return View(assistant);
        }

        // POST: ClinicManager/EditAssistant/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAssistant(int id, DoctorAssist assistant)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            if (id != assistant.Id) return NotFound();

            var doctorIds = await GetGroupDoctorIdsAsync();
            var existingAssist = await _context.DoctorAssists
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id && doctorIds.Contains(a.DoctorId));

            if (existingAssist == null)
            {
                TempData["Error"] = "Assistant not found or not in your group";
                return RedirectToAction(nameof(Assistants));
            }

            if (!doctorIds.Contains(assistant.DoctorId))
            {
                TempData["Error"] = "Selected doctor is not in your group";
                await PopulateGroupDoctors(existingAssist.DoctorId);
                return View(assistant);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (!string.IsNullOrEmpty(assistant.LoginPassword))
                    {
                        try { assistant.LoginPassword = BCrypt.Net.BCrypt.HashPassword(assistant.LoginPassword); }
                        catch { }
                    }
                    else
                    {
                        assistant.LoginPassword = existingAssist.LoginPassword;
                    }

                    assistant.LastLoginDate = existingAssist.LastLoginDate;
                    _context.Update(assistant);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Assistant updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DoctorAssists.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Assistants));
            }

            await PopulateGroupDoctors(assistant.DoctorId);
            return View(assistant);
        }

        // GET: ClinicManager/DeleteAssistant/5
        public async Task<IActionResult> DeleteAssistant(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorIds = await GetGroupDoctorIdsAsync();
            var assistant = await _context.DoctorAssists
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.Id == id && doctorIds.Contains(a.DoctorId));

            if (assistant == null)
            {
                TempData["Error"] = "Assistant not found or not in your group";
                return RedirectToAction(nameof(Assistants));
            }

            return View(assistant);
        }

        // POST: ClinicManager/DeleteAssistant/5
        [HttpPost, ActionName("DeleteAssistant")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAssistantConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var doctorIds = await GetGroupDoctorIdsAsync();
            var assistant = await _context.DoctorAssists
                .FirstOrDefaultAsync(a => a.Id == id && doctorIds.Contains(a.DoctorId));

            if (assistant != null)
            {
                _context.DoctorAssists.Remove(assistant);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Assistant deleted successfully";
            }

            return RedirectToAction(nameof(Assistants));
        }

        // GET: ClinicManager/Receptionists
        public async Task<IActionResult> Receptionists()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var doctorIds = await GetGroupDoctorIdsAsync();

            var receptionists = await _context.DoctorReceptions
                .Include(r => r.Doctor)
                .Where(r => doctorIds.Contains(r.DoctorId))
                .OrderBy(r => r.Doctor!.DoctorName)
                .ToListAsync();

            return View(receptionists);
        }

        // GET: ClinicManager/AddReceptionist
        public async Task<IActionResult> AddReceptionist()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            await PopulateGroupDoctors();
            return View();
        }

        // POST: ClinicManager/AddReceptionist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReceptionist(DoctorReception reception)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var doctorIds = await GetGroupDoctorIdsAsync();
            if (!doctorIds.Contains(reception.DoctorId))
            {
                TempData["Error"] = "Selected doctor is not in your group";
                await PopulateGroupDoctors();
                return View(reception);
            }

            if (!string.IsNullOrEmpty(reception.LoginUsername))
            {
                if (await _context.DoctorReceptions.AnyAsync(r => r.LoginUsername == reception.LoginUsername))
                {
                    ModelState.AddModelError("LoginUsername", "This username is already taken");
                    await PopulateGroupDoctors(reception.DoctorId);
                    return View(reception);
                }
            }

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(reception.LoginPassword))
                {
                    try { reception.LoginPassword = BCrypt.Net.BCrypt.HashPassword(reception.LoginPassword); }
                    catch { }
                }

                _context.Add(reception);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Receptionist added successfully";
                return RedirectToAction(nameof(Receptionists));
            }

            await PopulateGroupDoctors(reception.DoctorId);
            return View(reception);
        }

        // GET: ClinicManager/EditReceptionist/5
        public async Task<IActionResult> EditReceptionist(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorIds = await GetGroupDoctorIdsAsync();
            var reception = await _context.DoctorReceptions
                .FirstOrDefaultAsync(r => r.Id == id && doctorIds.Contains(r.DoctorId));

            if (reception == null)
            {
                TempData["Error"] = "Receptionist not found or not in your group";
                return RedirectToAction(nameof(Receptionists));
            }

            reception.LoginPassword = "";
            await PopulateGroupDoctors(reception.DoctorId);
            return View(reception);
        }

        // POST: ClinicManager/EditReceptionist/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditReceptionist(int id, DoctorReception reception)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            if (id != reception.Id) return NotFound();

            var doctorIds = await GetGroupDoctorIdsAsync();
            var existingReception = await _context.DoctorReceptions
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && doctorIds.Contains(r.DoctorId));

            if (existingReception == null)
            {
                TempData["Error"] = "Receptionist not found or not in your group";
                return RedirectToAction(nameof(Receptionists));
            }

            if (!doctorIds.Contains(reception.DoctorId))
            {
                TempData["Error"] = "Selected doctor is not in your group";
                await PopulateGroupDoctors(existingReception.DoctorId);
                return View(reception);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (!string.IsNullOrEmpty(reception.LoginPassword))
                    {
                        try { reception.LoginPassword = BCrypt.Net.BCrypt.HashPassword(reception.LoginPassword); }
                        catch { }
                    }
                    else
                    {
                        reception.LoginPassword = existingReception.LoginPassword;
                    }

                    reception.LastLoginDate = existingReception.LastLoginDate;
                    _context.Update(reception);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Receptionist updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DoctorReceptions.Any(e => e.Id == id)) return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Receptionists));
            }

            await PopulateGroupDoctors(reception.DoctorId);
            return View(reception);
        }

        // GET: ClinicManager/DeleteReceptionist/5
        public async Task<IActionResult> DeleteReceptionist(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            if (id == null) return NotFound();

            var doctorIds = await GetGroupDoctorIdsAsync();
            var reception = await _context.DoctorReceptions
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Id == id && doctorIds.Contains(r.DoctorId));

            if (reception == null)
            {
                TempData["Error"] = "Receptionist not found or not in your group";
                return RedirectToAction(nameof(Receptionists));
            }

            return View(reception);
        }

        // POST: ClinicManager/DeleteReceptionist/5
        [HttpPost, ActionName("DeleteReceptionist")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReceptionistConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");
            if (!IsClinicManager())
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction("Index", "Home");
            }

            var doctorIds = await GetGroupDoctorIdsAsync();
            var reception = await _context.DoctorReceptions
                .FirstOrDefaultAsync(r => r.Id == id && doctorIds.Contains(r.DoctorId));

            if (reception != null)
            {
                _context.DoctorReceptions.Remove(reception);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Receptionist deleted successfully";
            }

            return RedirectToAction(nameof(Receptionists));
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private async Task<List<int>> GetGroupDoctorIdsAsync()
        {
            var groupId = GetGroupId();
            if (!groupId.HasValue) return new List<int>();

            return await _context.DoctorGroupMembers
                .Where(m => m.DoctorGroupId == groupId.Value)
                .Select(m => m.DoctorId)
                .ToListAsync();
        }

        private void PopulateDoctorDropdowns(int? selectedSpecialist = null, int? selectedDepartment = null)
        {
            ViewBag.DepartmentId = new SelectList(_context.Departments.OrderBy(d => d.DepartmentName), "Id", "DepartmentName", selectedDepartment);
            ViewBag.SpecialistId = new SelectList(_context.Specialists.OrderBy(d => d.SpecialistName), "Id", "SpecialistName", selectedSpecialist);

            var specialistsData = _context.Specialists.OrderBy(d => d.SpecialistName).Select(s => new
            {
                id = s.Id,
                name = s.SpecialistName,
                departmentId = s.DepartmentId
            }).ToList();
            ViewBag.SpecialistsJson = System.Text.Json.JsonSerializer.Serialize(specialistsData);
        }

        private async Task PopulateGroupDoctors(int? selectedDoctorId = null)
        {
            var groupId = GetGroupId();
            List<DoctorInfo> groupDoctors = new();

            if (groupId.HasValue)
            {
                var doctorIds = await _context.DoctorGroupMembers
                    .Where(m => m.DoctorGroupId == groupId.Value)
                    .Select(m => m.DoctorId)
                    .ToListAsync();

                groupDoctors = await _context.DoctorInfos
                    .Where(d => doctorIds.Contains(d.Id) && d.Active)
                    .OrderBy(d => d.DoctorName)
                    .ToListAsync();
            }

            ViewBag.DoctorId = new SelectList(groupDoctors, "Id", "DoctorName", selectedDoctorId);
        }
    }
}
