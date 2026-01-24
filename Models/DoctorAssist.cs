using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class DoctorAssist
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Doctor is required")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Assistant name is required")]
        [StringLength(100)]
        [Display(Name = "Assistant Name")]
        public string AssistName { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Phone 1")]
        public string? AssistTel1 { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone 2")]
        public string? AssistTel2 { get; set; }

        [StringLength(200)]
        [Display(Name = "Address")]
        public string? AssistAddress { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // Login Fields
        [StringLength(50)]
        [Display(Name = "Username")]
        public string? LoginUsername { get; set; }

        [StringLength(255)]
        [Display(Name = "Password")]
        public string? LoginPassword { get; set; }

        [Display(Name = "Can Login")]
        public bool CanLogin { get; set; } = false;

        [Display(Name = "Last Login Date")]
        public DateTime? LastLoginDate { get; set; }

        // Navigation Properties
        [ForeignKey("DoctorId")]
        public virtual DoctorInfo? Doctor { get; set; } = null!;
    }
}
