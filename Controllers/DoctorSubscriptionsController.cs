using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class DoctorSubscriptionsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorSubscriptionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DoctorSubscriptions?doctorId=1
        public async Task<IActionResult> Index(int? doctorId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (doctorId == null)
                return RedirectToAction("Index", "DoctorInfos");

            var doctor = await _context.DoctorInfos
                .Include(d => d.Subscriptions)
                .FirstOrDefaultAsync(d => d.Id == doctorId);

            if (doctor == null)
                return NotFound();

            ViewBag.DoctorId = doctorId;
            ViewBag.DoctorName = doctor.DoctorName;

            return View(doctor.Subscriptions.OrderByDescending(s => s.StartDate).ToList());
        }

        // GET: DoctorSubscriptions/Create?doctorId=1
        public async Task<IActionResult> Create(int? doctorId)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (doctorId == null)
                return RedirectToAction("Index", "DoctorInfos");

            var doctor = await _context.DoctorInfos.FindAsync(doctorId);
            if (doctor == null)
                return NotFound();

            ViewBag.DoctorId = doctorId;
            ViewBag.DoctorName = doctor.DoctorName;

            // Set default dates
            var subscription = new DoctorSubscription
            {
                DoctorId = doctorId.Value,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(1),
                IsActive = true
            };

            return View(subscription);
        }

        // POST: DoctorSubscriptions/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DoctorSubscription subscription)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            // Validate DoctorId
            if (subscription.DoctorId <= 0)
            {
                ModelState.AddModelError("DoctorId", "Invalid doctor selected");
            }

            // Validate dates
            if (subscription.EndDate <= subscription.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            // Remove Doctor navigation property from validation
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                try
                {
                    subscription.CreatedDate = DateTime.Now;
                    _context.Add(subscription);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Subscription created successfully";
                    return RedirectToAction(nameof(Index), new { doctorId = subscription.DoctorId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error saving subscription: {ex.Message}");
                }
            }

            var doctor = await _context.DoctorInfos.FindAsync(subscription.DoctorId);
            ViewBag.DoctorId = subscription.DoctorId;
            ViewBag.DoctorName = doctor?.DoctorName ?? "Unknown";
            return View(subscription);
        }

        // GET: DoctorSubscriptions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var subscription = await _context.DoctorSubscriptions
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null)
                return NotFound();

            ViewBag.DoctorId = subscription.DoctorId;
            ViewBag.DoctorName = subscription.Doctor.DoctorName;

            return View(subscription);
        }

        // POST: DoctorSubscriptions/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DoctorSubscription subscription)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id != subscription.Id)
                return NotFound();

            // Validate dates
            if (subscription.EndDate <= subscription.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date");
            }

            // Remove Doctor navigation property from validation
            ModelState.Remove("Doctor");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingSubscription = await _context.DoctorSubscriptions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == id);

                    subscription.CreatedDate = existingSubscription?.CreatedDate ?? DateTime.Now;
                    subscription.ModifiedDate = DateTime.Now;

                    _context.Update(subscription);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Subscription updated successfully";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubscriptionExists(subscription.Id))
                        return NotFound();
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating subscription: {ex.Message}");
                    var doctor2 = await _context.DoctorInfos.FindAsync(subscription.DoctorId);
                    ViewBag.DoctorId = subscription.DoctorId;
                    ViewBag.DoctorName = doctor2?.DoctorName ?? "Unknown";
                    return View(subscription);
                }
                return RedirectToAction(nameof(Index), new { doctorId = subscription.DoctorId });
            }

            var doctor = await _context.DoctorInfos.FindAsync(subscription.DoctorId);
            ViewBag.DoctorId = subscription.DoctorId;
            ViewBag.DoctorName = doctor?.DoctorName ?? "Unknown";
            return View(subscription);
        }

        // GET: DoctorSubscriptions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            if (id == null)
                return NotFound();

            var subscription = await _context.DoctorSubscriptions
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subscription == null)
                return NotFound();

            return View(subscription);
        }

        // POST: DoctorSubscriptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            var subscription = await _context.DoctorSubscriptions.FindAsync(id);
            if (subscription != null)
            {
                var doctorId = subscription.DoctorId;
                _context.DoctorSubscriptions.Remove(subscription);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Subscription deleted successfully";
                return RedirectToAction(nameof(Index), new { doctorId = doctorId });
            }

            return RedirectToAction("Index", "DoctorInfos");
        }

        private bool SubscriptionExists(int id)
        {
            return _context.DoctorSubscriptions.Any(e => e.Id == id);
        }
    }
}
