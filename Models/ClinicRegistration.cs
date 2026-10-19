using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class ClinicRegistration
{
    [Required, StringLength(100), Display(Name = "Clinic Name")]
    public string Name { get; set; } = string.Empty;
    [StringLength(200), Display(Name = "Address")]
    public string? Address { get; set; }
    [Required, StringLength(20), Display(Name = "Phone")]
    public string Phone { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(100), Display(Name = "Email Address")]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(100), Display(Name = "Clinic Manager Name")]
    public string ManagerName { get; set; } = string.Empty;
    [Required, StringLength(50), RegularExpression(@"[a-zA-Z0-9_.-]+"), Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;
    [Required, StringLength(72, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;
    [Required, Compare(nameof(Password)), DataType(DataType.Password), Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ClinicDetails
{
    public Clinic Clinic { get; set; } = null!;
    public List<UserInfo> Managers { get; set; } = new();
    public List<DoctorInfo> Doctors { get; set; } = new();
    public List<DoctorAssist> Assistants { get; set; } = new();
    public List<DoctorReception> Receptionists { get; set; } = new();
    public List<Patient> Patients { get; set; } = new();
}