using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class UserInfo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string UserPassword { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string? UserFullName { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone")]
        public string? UserTel { get; set; }

        [StringLength(50)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [Display(Name = "Role")]
        public int? RoleId { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // Navigation Properties
        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }

        public virtual ICollection<DoctorInfo> Doctors { get; set; } = new List<DoctorInfo>();
    }
}
