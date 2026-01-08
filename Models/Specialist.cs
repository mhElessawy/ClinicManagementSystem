using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class Specialist
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Specialist name is required")]
        [StringLength(100)]
        [Display(Name = "Specialist Name")]
        public string SpecialistName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Navigation Properties
        [ForeignKey("DepartmentId")]
        public virtual Department? Department { get; set; } = null!;

        public virtual ICollection<DoctorInfo>? Doctors { get; set; } = new List<DoctorInfo>();
    }
}
