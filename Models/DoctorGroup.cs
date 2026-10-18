using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class DoctorGroup : IClinicOwned
    {
        [Key]
        public int Id { get; set; }

        public int? ClinicId { get; set; }
        public virtual Clinic? Clinic { get; set; }

        [Required(ErrorMessage = "Group name is required")]
        [StringLength(100)]
        [Display(Name = "Group Name")]
        public string GroupName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<DoctorGroupMember> Members { get; set; } = new List<DoctorGroupMember>();
    }
}