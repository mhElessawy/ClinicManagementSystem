using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Permissions
        [Display(Name = "Can Manage Departments")]
        public bool CanManageDepartments { get; set; }

        [Display(Name = "Can Manage Specialists")]
        public bool CanManageSpecialists { get; set; }

        [Display(Name = "Can Manage Doctors")]
        public bool CanManageDoctors { get; set; }

        [Display(Name = "Can Manage Patients")]
        public bool CanManagePatients { get; set; }

        [Display(Name = "Can Manage Diagnoses")]
        public bool CanManageDiagnoses { get; set; }

        [Display(Name = "Can Manage Users")]
        public bool CanManageUsers { get; set; }

        [Display(Name = "Can View Reports")]
        public bool CanViewReports { get; set; }

        [Display(Name = "Can Manage Assistants")]
        public bool CanManageAssistants { get; set; }

        [Display(Name = "View All Patients")]
        public bool ViewAllPatients { get; set; }

        [Display(Name = "View Own Patients Only")]
        public bool ViewOwnPatientsOnly { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // Navigation properties
        public virtual ICollection<UserInfo> Users { get; set; } = new List<UserInfo>();
    }
}
