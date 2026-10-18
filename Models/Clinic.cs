using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class Clinic
{
    public int Id { get; set; }
    [Required, StringLength(100), Display(Name = "اسم العيادة")]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(32), Display(Name = "كود العيادة")]
    public string Code { get; set; } = string.Empty;
    [StringLength(200), Display(Name = "العنوان")]
    public string? Address { get; set; }
    [Required, StringLength(20), Display(Name = "التليفون")]
    public string Phone { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(100), Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public interface IClinicOwned
{
    int? ClinicId { get; set; }
    Clinic? Clinic { get; set; }
}
