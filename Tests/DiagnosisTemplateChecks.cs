using ClinicManagementSystem.Controllers;
using ClinicManagementSystem.Helpers;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

static class DiagnosisTemplateChecks
{
    public static async Task Run(DbContextOptions<ApplicationDbContext> options, HttpContext http,
        List<Clinic> clinics, List<DoctorInfo> doctors)
    {
        void Login(int clinic, string type = SessionHelper.TYPE_CLINIC_MANAGER)
        {
            SessionHelper.SetUserSession(http.Session, doctors[clinic].Id, "test", type, "Test", doctorId: doctors[clinic].Id);
            http.Session.SetInt32("ClinicId", clinics[clinic].Id);
        }
        void Check(bool value, string label)
        {
            if (!value) throw new Exception("FAIL: " + label);
            Console.WriteLine("PASS: " + label);
        }
        ApplicationDbContext Open() => new(options, new HttpContextAccessor { HttpContext = http });
        ClinicManagerController Manager(ApplicationDbContext db) => new(db, null!, null!)
        {
            ControllerContext = new ControllerContext { HttpContext = http },
            TempData = new TempDataDictionary(http, new TestTempDataProvider())
        };
        Login(1);
        await using (var db = Open())
        {
            (await db.DoctorInfos.SingleAsync(d => d.Id == doctors[1].Id)).SpecialistId = 2;
            await db.SaveChangesAsync();
        }
        Login(0);
        int id;
        await using (var db = Open())
        {
            var manager = Manager(db);
            var form = (ViewResult)await manager.EditDiagnosisTemplate(null);
            var specialties = (SelectList)form.ViewData["Specialties"]!;
            Check(specialties.Select(s => s.Value).SequenceEqual(new[] { "1" }), "Template dropdown lists only current clinic specialties");
            Check(await manager.EditDiagnosisTemplate(0, new DiagnosisTemplate { Name = "General", Content = "Clinic text" }) is RedirectToActionResult, "Manager creates clinic template");
            id = await db.DiagnosisTemplates.Select(t => t.Id).SingleAsync();
            Check((await db.DiagnosisTemplates.FindAsync(id))!.ClinicId == clinics[0].Id, "Template belongs to current clinic");
            Check(await manager.EditDiagnosisTemplate(0, new DiagnosisTemplate { Name = "Foreign specialty", Content = "Rejected", SpecialistId = 2 }) is ViewResult
                && !manager.ModelState.IsValid, "Existing specialty from another clinic cannot be posted");
            Check(await db.DiagnosisTemplates.CountAsync() == 1, "Rejected specialty does not create a template");
            manager.ModelState.Clear();
            Check(await manager.EditDiagnosisTemplate(id, new DiagnosisTemplate { Name = "Invalid edit", Content = "Rejected", SpecialistId = 2 }) is ViewResult
                && !manager.ModelState.IsValid, "Template edit rejects specialties outside current clinic");
            Check((await db.DiagnosisTemplates.FindAsync(id))!.SpecialistId == null, "Rejected edit preserves original template scope");
            manager.ModelState.Clear();
            db.DiagnosisTemplates.AddRange(
                new DiagnosisTemplate { Name = "Matching", Content = "Specialty text", SpecialistId = 1 },
                new DiagnosisTemplate { Name = "Other", Content = "Other specialty", SpecialistId = 2 },
                new DiagnosisTemplate { Name = "Inactive", Content = "Hidden", Active = false });
            await db.SaveChangesAsync();
            await manager.EditDiagnosisTemplate(id, new DiagnosisTemplate { Name = "Updated", Content = "Updated text" });
            Check((await db.DiagnosisTemplates.FindAsync(id))!.Content == "Updated text", "Template changes persist");
        }
        Login(1);
        await using (var db = Open())
        {
            Check(!await db.DiagnosisTemplates.AnyAsync(), "Another clinic cannot see templates");
            Check(await Manager(db).EditDiagnosisTemplate(id) is NotFoundResult, "Cross-clinic template edit blocked");
            Check(await Manager(db).DeleteDiagnosisTemplate(id) is NotFoundResult, "Cross-clinic template deletion blocked");
        }
        Login(0, SessionHelper.TYPE_DOCTOR);
        await using (var db = Open())
        {
            var controller = new PatientDiagnosesController(db) { ControllerContext = new ControllerContext { HttpContext = http } };
            var result = (JsonResult)await controller.Templates(doctors[1].Id);
            var json = JsonSerializer.Serialize(result.Value);
            Check(json.Contains("Updated") && json.Contains("Matching") && !json.Contains("Other") && !json.Contains("Inactive"), "Doctor sees active clinic and specialty templates; posted doctor cannot override session");
            Check(await Manager(db).DiagnosisTemplates() is ForbidResult, "Doctor cannot manage templates");
        }
        Login(0);
        await using (var db = Open())
        {
            var controller = new PatientDiagnosesController(db) { ControllerContext = new ControllerContext { HttpContext = http } };
            Check(await controller.Templates(doctors[1].Id) is NotFoundResult, "Template lookup rejects another clinic's doctor");
            var manager = Manager(db);
            Check(await manager.EditDiagnosisTemplate(0, new DiagnosisTemplate { Name = "Invalid", Content = "Invalid", SpecialistId = int.MaxValue }) is ViewResult && !manager.ModelState.IsValid, "Invalid specialty rejected");
            Check(await manager.DeleteDiagnosisTemplate(id) is RedirectToActionResult && !await db.DiagnosisTemplates.AnyAsync(t => t.Id == id), "Manager deletes template");
        }
    }
}
