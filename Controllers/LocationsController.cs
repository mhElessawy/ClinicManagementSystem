using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class LocationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LocationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Locations
        public async Task<IActionResult> Index()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);

            // Only doctors can access their locations
            if (userType != SessionHelper.TYPE_DOCTOR || !doctorId.HasValue)
            {
                TempData["Error"] = "Only doctors can manage locations";
                return RedirectToAction("Index", "Home");
            }

            var locations = await _context.Locations
                .Where(l => l.DoctorId == doctorId.Value)
                .OrderByDescending(l => l.Active)
                .ThenBy(l => l.LocationName)
                .ToListAsync();

            return View(locations);
        }

        // GET: Locations/Create
        public IActionResult Create()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var userType = SessionHelper.GetUserType(HttpContext.Session);
            if (userType != SessionHelper.TYPE_DOCTOR)
            {
                TempData["Error"] = "Only doctors can create locations";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Locations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Location location)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
            {
                TempData["Error"] = "Doctor ID not found";
                return RedirectToAction("Index", "Home");
            }

            // Set the doctor ID
            location.DoctorId = doctorId.Value;

            if (ModelState.IsValid)
            {
                _context.Add(location);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Location created successfully";
                return RedirectToAction(nameof(Index));
            }

            return View(location);
        }

        // GET: Locations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.Id == id && l.DoctorId == doctorId.Value);

            if (location == null)
            {
                TempData["Error"] = "Location not found or you don't have permission";
                return RedirectToAction(nameof(Index));
            }

            return View(location);
        }

        // POST: Locations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Location location)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != location.Id)
                return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            // Ensure the location belongs to the current doctor
            var existingLocation = await _context.Locations
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id && l.DoctorId == doctorId.Value);

            if (existingLocation == null)
            {
                TempData["Error"] = "Location not found or you don't have permission";
                return RedirectToAction(nameof(Index));
            }

            // Set the doctor ID to prevent tampering
            location.DoctorId = doctorId.Value;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(location);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Location updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LocationExists(location.Id))
                        return NotFound();
                    else
                        throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(location);
        }

        // GET: Locations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            var location = await _context.Locations
                .Include(l => l.Appointments)
                .FirstOrDefaultAsync(l => l.Id == id && l.DoctorId == doctorId.Value);

            if (location == null)
            {
                TempData["Error"] = "Location not found or you don't have permission";
                return RedirectToAction(nameof(Index));
            }

            return View(location);
        }

        // POST: Locations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var doctorId = SessionHelper.GetDoctorId(HttpContext.Session);
            if (!doctorId.HasValue)
                return RedirectToAction("Index", "Home");

            var location = await _context.Locations
                .Include(l => l.Appointments)
                .FirstOrDefaultAsync(l => l.Id == id && l.DoctorId == doctorId.Value);

            if (location == null)
            {
                TempData["Error"] = "Location not found or you don't have permission";
                return RedirectToAction(nameof(Index));
            }

            // Check if location has appointments
            if (location.Appointments.Any())
            {
                TempData["Error"] = "Cannot delete location with existing appointments. Please delete or reassign appointments first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Locations.Remove(location);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Location deleted successfully";
            return RedirectToAction(nameof(Index));
        }

        private bool LocationExists(int id)
        {
            return _context.Locations.Any(e => e.Id == id);
        }
    }
}
