using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class Clinic
{
    public int Id { get; set; }
    [Required, StringLength(100), Display(Name = "Clinic Name")]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(32), Display(Name = "Clinic Code")]
    public string Code { get; set; } = string.Empty;
    [StringLength(200), Display(Name = "Address")]
    public string? Address { get; set; }
    [Required, StringLength(20), Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(100), Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public interface IClinicOwned
{
    int? ClinicId { get; set; }
    Clinic? Clinic { get; set; }
}
