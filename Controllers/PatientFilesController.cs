using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Controllers;

public class PatientFilesController(ApplicationDbContext db, IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Download(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || Path.GetFileName(fileName) != fileName || fileName.Contains('\\'))
            return NotFound();
        var url = "/PatientFile/" + fileName;
        if (!await db.PatientDiagnoses.AnyAsync(d => d.DiagnosisFilePath == url)) return NotFound();
        var path = Path.Combine(environment.WebRootPath, "PatientFile", fileName);
        if (!System.IO.File.Exists(path)) return NotFound();
        Response.Headers.CacheControl = "no-store, private";
        new FileExtensionContentTypeProvider().TryGetContentType(path, out var contentType);
        return PhysicalFile(path, contentType ?? "application/octet-stream");
    }
}
