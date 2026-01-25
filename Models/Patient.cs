using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Patient name is required")]
        [StringLength(100)]
        [Display(Name = "Patient Name")]
        public string PatientName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Civil ID")]
        public string? PatientCivilID { get; set; }

        [Display(Name = "Birth Date")]
        public DateTime? BirthDate { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone 1")]
        public string? PatientTel1 { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone 2")]
        public string? PatientTel2 { get; set; }

        [StringLength(200)]
        [Display(Name = "Address")]
        public string? PatientAddress { get; set; }

        [Display(Name = "Doctor")]
        public int? DoctorId { get; set; }

        // Navigation Properties
        [ForeignKey("DoctorId")]
        public virtual DoctorInfo? Doctor { get; set; }

        public virtual ICollection<PatientDiagnosis> PatientDiagnoses { get; set; } = new List<PatientDiagnosis>();

        // Computed property for Age
        [NotMapped]
        public int? Age
        {
            get
            {
                if (BirthDate == null)
                    return null;

                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;

                // Check if birthday hasn't occurred yet this year
                if (BirthDate.Value.Date > today.AddYears(-age))
                    age--;

                return age;
            }
        }
    }
}

