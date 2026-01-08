using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public Patient? Patient { get; set; }

        [Required]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public DoctorInfo? Doctor { get; set; }

        [Required]
        [Display(Name = "Appointment Date")]
        [DataType(DataType.Date)]
        public DateTime AppointmentDate { get; set; }

        [Required]
        [Display(Name = "Appointment Time")]
        [DataType(DataType.Time)]
        public TimeSpan AppointmentTime { get; set; }

        [Required]
        [Display(Name = "Reason for Appointment")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Display(Name = "Notes")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        [Display(Name = "Status")]
        [StringLength(50)]
        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled, NoShow

        [Display(Name = "Created By")]
        public int CreatedBy { get; set; } // UserInfo.Id or DoctorInfo.Id or DoctorAssist.Id

        [Display(Name = "Created By Type")]
        [StringLength(20)]
        public string CreatedByType { get; set; } = string.Empty; // Admin, Doctor, Assistant

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        [Display(Name = "Deleted")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Deletion Reason")]
        [StringLength(500)]
        public string? DeletionReason { get; set; }

        [Display(Name = "Deleted By")]
        public int? DeletedBy { get; set; }

        [Display(Name = "Deleted By Type")]
        [StringLength(20)]
        public string? DeletedByType { get; set; }

        [Display(Name = "Deletion Date")]
        public DateTime? DeletionDate { get; set; }
    }
}
