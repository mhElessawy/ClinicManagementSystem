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
        clinic.Active = !clinic.Active;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
}
