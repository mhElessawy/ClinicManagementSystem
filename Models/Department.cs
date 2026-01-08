using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Department name is required")]
        [StringLength(100)]
        [Display(Name = "Department Name")]
        public string DepartmentName { get; set; } = string.Empty;

        // Navigation Properties
        public virtual ICollection<Specialist> Specialists { get; set; } = new List<Specialist>();
    }
}
