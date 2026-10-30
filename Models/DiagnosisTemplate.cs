using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class DiagnosisTemplate : IClinicOwned
{
    public int Id { get; set; }
    public int? ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    [Required, StringLength(100), Display(Name = "Template Name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(1000), Display(Name = "Diagnosis Text")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Specialty")]
    public int? SpecialistId { get; set; }
    public Specialist? Specialist { get; set; }

    public bool Active { get; set; } = true;
}