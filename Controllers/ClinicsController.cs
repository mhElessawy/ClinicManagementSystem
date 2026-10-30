using ClinicManagementSystem.Helpers;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

// Access is restricted to the site administrator by ClinicAccessFilter.
public class ClinicsController(ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Clinics.OrderBy(c => c.Name).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var clinic = await db.Clinics.SingleOrDefaultAsync(c => c.Id == id);
        if (clinic == null) return NotFound();
        return View(new ClinicDetails
        {
            Clinic = clinic,
            Managers = await db.UserInfos.Include(u => u.Role).Where(u => u.ClinicId == id).ToListAsync(),
            Doctors = await db.DoctorInfos.Include(d => d.Specialist).Where(d => d.ClinicId == id).ToListAsync(),
            Assistants = await db.DoctorAssists.Include(a => a.Doctor).Where(a => a.ClinicId == id).ToListAsync(),
            Receptionists = await db.DoctorReceptions.Include(r => r.Doctor).Where(r => r.ClinicId == id).ToListAsync(),
            Patients = await db.Patients.Include(p => p.Doctor).Where(p => p.ClinicId == id).ToListAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var clinic = await db.Clinics.FindAsync(id);
        if (clinic == null) return NotFound();
        if (!clinic.Active && (clinic.SubscriptionEndsOn == null || clinic.SubscriptionEndsOn < ClinicSubscription.Today))
            return RedirectToAction(nameof(Subscription), new { id });
        clinic.Active = !clinic.Active;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Subscription(int id)
    {
        var clinic = await db.Clinics.FindAsync(id);
        if (clinic == null) return NotFound();
        ViewBag.ClinicName = clinic.Name;
        return View(new ClinicSubscriptionUpdate
        {
            Active = clinic.Active,
            Period = clinic.SubscriptionEndsOn.HasValue ? "custom" : "1",
            SubscriptionEndsOn = clinic.SubscriptionEndsOn
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Subscription(int id, ClinicSubscriptionUpdate update)
    {
        var clinic = await db.Clinics.FindAsync(id);
        if (clinic == null) return NotFound();
        var today = ClinicSubscription.Today;
        DateOnly? endsOn = update.SubscriptionEndsOn;
        if (update.Period == "custom")
        {
            if (endsOn == null)
                ModelState.AddModelError(nameof(update.SubscriptionEndsOn), "Select a subscription end date.");
            else if (update.Active && endsOn < today)
                ModelState.AddModelError(nameof(update.SubscriptionEndsOn), "An active subscription must end today or later.");
        }
        else if (update.Period is "1" or "3" or "6" or "12")
        {
            // Preserve remaining paid time when renewing an unexpired subscription.
            var start = clinic.SubscriptionEndsOn is { } current && current >= today ? current : today;
            if (start.Year >= 9998)
                ModelState.AddModelError(nameof(update.Period), "The current end date is too far in the future to extend.");
            else
                endsOn = start.AddMonths(int.Parse(update.Period));
        }
        else
            ModelState.AddModelError(nameof(update.Period), "Select a valid subscription period.");

        if (!ModelState.IsValid)
        {
            ViewBag.ClinicName = clinic.Name;
            return View(update);
        }
        clinic.Active = update.Active;
        clinic.SubscriptionEndsOn = endsOn;
        await db.SaveChangesAsync();
        TempData["Success"] = "Clinic subscription updated successfully.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
