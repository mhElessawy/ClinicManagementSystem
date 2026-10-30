using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

public partial class ClinicManagerController
{
    public async Task<IActionResult> DiagnosisTemplates()
    {
        if (!IsClinicManager() || !_context.CurrentClinicId.HasValue) return Forbid();
        return View(await _context.DiagnosisTemplates.Include(t => t.Specialist)
            .OrderBy(t => t.Name).ToListAsync());
    }

    public async Task<IActionResult> EditDiagnosisTemplate(int? id)
    {
        if (!IsClinicManager() || !_context.CurrentClinicId.HasValue) return Forbid();
        var template = id.HasValue ? await _context.DiagnosisTemplates.FirstOrDefaultAsync(t => t.Id == id) : new DiagnosisTemplate();
        if (template == null) return NotFound();
        await PopulateTemplateSpecialties(template.SpecialistId);
        return View(template);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDiagnosisTemplate(int id,
        [Bind("Name,Content,SpecialistId,Active")] DiagnosisTemplate input)
    {
        if (!IsClinicManager() || !_context.CurrentClinicId.HasValue) return Forbid();
        var template = id == 0 ? new DiagnosisTemplate() : await _context.DiagnosisTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();
        if (input.SpecialistId.HasValue && !await ClinicTemplateSpecialties().AnyAsync(s => s.Id == input.SpecialistId))
            ModelState.AddModelError(nameof(input.SpecialistId), "Select a specialty available in your clinic.");
        if (!ModelState.IsValid)
        {
            input.Id = id;
            await PopulateTemplateSpecialties(input.SpecialistId);
            return View(input);
        }
        template.Name = input.Name.Trim();
        template.Content = input.Content.Trim();
        template.SpecialistId = input.SpecialistId;
        template.Active = input.Active;
        if (id == 0) _context.DiagnosisTemplates.Add(template);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Diagnosis template saved successfully.";
        return RedirectToAction(nameof(DiagnosisTemplates));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDiagnosisTemplate(int id)
    {
        if (!IsClinicManager() || !_context.CurrentClinicId.HasValue) return Forbid();
        var template = await _context.DiagnosisTemplates.FirstOrDefaultAsync(t => t.Id == id);
        if (template == null) return NotFound();
        _context.DiagnosisTemplates.Remove(template);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Diagnosis template deleted successfully.";
        return RedirectToAction(nameof(DiagnosisTemplates));
    }

    private IQueryable<Specialist> ClinicTemplateSpecialties()
    {
        var clinicId = _context.CurrentClinicId;
        return _context.Specialists.Where(s => _context.DoctorInfos
            .Any(d => d.ClinicId == clinicId && d.SpecialistId == s.Id));
    }

    private async Task PopulateTemplateSpecialties(int? selected)
    {
        ViewBag.Specialties = new SelectList(await ClinicTemplateSpecialties().OrderBy(s => s.SpecialistName).ToListAsync(),
            "Id", "SpecialistName", selected);
    }
}
