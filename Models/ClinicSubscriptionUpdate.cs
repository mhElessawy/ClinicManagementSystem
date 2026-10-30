using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models;

public class ClinicSubscriptionUpdate
{
    public bool Active { get; set; }
    [Required]
    public string Period { get; set; } = "1";
    [DataType(DataType.Date)]
    public DateOnly? SubscriptionEndsOn { get; set; }
}
