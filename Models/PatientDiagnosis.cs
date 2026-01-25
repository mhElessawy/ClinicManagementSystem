using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class PatientDiagnosis
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Patient is required")]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Display(Name = "Doctor")]
        public int? DoctorId { get; set; }

        [Display(Name = "Appointment")]
        public int? AppointmentId { get; set; }

        [Required(ErrorMessage = "Diagnosis date is required")]
        [Display(Name = "Diagnosis Date")]
        public DateTime DiagnosisDate { get; set; } = DateTime.Now;

        [StringLength(1000)]
        [Display(Name = "Diagnosis Details")]
        public string? DiagnosisDetails { get; set; }

        [StringLength(500)]
        [Display(Name = "Diagnosis File Path")]
        public string? DiagnosisFilePath { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // Navigation Properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual DoctorInfo? Doctor { get; set; }

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }
    }
}
