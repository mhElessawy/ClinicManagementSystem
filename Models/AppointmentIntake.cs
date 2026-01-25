using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class AppointmentIntake
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Appointment")]
        public int AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }

        // Patient Vitals (optional)
        [Display(Name = "Blood Pressure")]
        [StringLength(20)]
        public string? BloodPressure { get; set; }

        [Display(Name = "Heart Rate (bpm)")]
        public int? HeartRate { get; set; }

        [Display(Name = "Temperature (C)")]
        public decimal? Temperature { get; set; }

        [Display(Name = "Weight (kg)")]
        public decimal? Weight { get; set; }

        [Display(Name = "Height (cm)")]
        public decimal? Height { get; set; }

        // Chief Complaint / Current Symptoms
        [Display(Name = "Chief Complaint")]
        [StringLength(500)]
        public string? ChiefComplaint { get; set; }

        [Display(Name = "Current Symptoms")]
        [StringLength(1000)]
        public string? CurrentSymptoms { get; set; }

        [Display(Name = "Symptom Duration")]
        [StringLength(100)]
        public string? SymptomDuration { get; set; }

        [Display(Name = "Pain Level (1-10)")]
        [Range(1, 10)]
        public int? PainLevel { get; set; }

        // Medical History Questions
        [Display(Name = "Current Medications")]
        [StringLength(500)]
        public string? CurrentMedications { get; set; }

        [Display(Name = "Known Allergies")]
        [StringLength(500)]
        public string? Allergies { get; set; }

        [Display(Name = "Previous Medical Conditions")]
        [StringLength(500)]
        public string? PreviousConditions { get; set; }

        // Specialty-Specific Questions Responses (stored as JSON)
        [Display(Name = "Specialty Questions Responses")]
        public string? SpecialtyQuestionsJson { get; set; }

        // Additional Notes from Assistant
        [Display(Name = "Additional Notes")]
        [StringLength(2000)]
        public string? AdditionalNotes { get; set; }

        // Patient Arrival Status
        [Display(Name = "Patient Arrived")]
        public bool PatientArrived { get; set; } = true;

        [Display(Name = "Arrival Time")]
        public DateTime? ArrivalTime { get; set; }

        // Ready for Doctor
        [Display(Name = "Ready for Doctor")]
        public bool ReadyForDoctor { get; set; } = false;

        // Tracking Info
        [Display(Name = "Performed By")]
        public int PerformedBy { get; set; } // DoctorAssist.Id

        [Display(Name = "Performed By Name")]
        [StringLength(100)]
        public string? PerformedByName { get; set; }

        [Display(Name = "Intake Date")]
        public DateTime IntakeDate { get; set; } = DateTime.Now;

        [Display(Name = "Last Updated")]
        public DateTime? LastUpdated { get; set; }
    }
}
