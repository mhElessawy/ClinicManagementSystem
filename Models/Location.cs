using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class Location
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Location name is required")]
        [StringLength(100)]
        [Display(Name = "Location Name")]
        public string LocationName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Address")]
        public string? Address { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone")]
        public string? Phone { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // Doctor-specific location
        [Required]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public virtual DoctorInfo? Doctor { get; set; }

        // Navigation Properties
        public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
