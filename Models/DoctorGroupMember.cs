using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class DoctorGroupMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Group")]
        public int DoctorGroupId { get; set; }

        [Required]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Display(Name = "Added Date")]
        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("DoctorGroupId")]
        public virtual DoctorGroup? DoctorGroup { get; set; }

        [ForeignKey("DoctorId")]
        public virtual DoctorInfo? Doctor { get; set; }
    }
}