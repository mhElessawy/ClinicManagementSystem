using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class DoctorInfo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Doctor name is required")]
        [StringLength(100)]
        [Display(Name = "Doctor Name")]
        public string DoctorName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Address")]
        public string? DoctorAddress { get; set; }

        [StringLength(50)]
        [Display(Name = "Title")]
        public string? DoctorTitle { get; set; }

        [Display(Name = "Specialist")]
        public int? SpecialistId { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        [StringLength(50)]
        [Display(Name = "Civil ID")]
        public string? DoctorCivilId { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone 1")]
        public string? DoctorTel1 { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone 2")]
        public string? DoctorTel2 { get; set; }

        [Display(Name = "User")]
        public int? UserId { get; set; }

        [StringLength(10)]
        [Display(Name = "Gender")]
        public string? Gender { get; set; }

        [StringLength(500)]
        [Display(Name = "Details")]
        public string? DoctorDetails { get; set; }

        [Display(Name = "Registration Date")]
        public DateTime? RegDate { get; set; }

        [Display(Name = "Picture")]
        public byte[]? DoctorPicture { get; set; }

        // Login Fields
        [StringLength(50)]
        [Display(Name = "Username")]
        public string? LoginUsername { get; set; }

        [StringLength(255)]
        [Display(Name = "Password")]
        public string? LoginPassword { get; set; }

        [Display(Name = "Can Login")]
        public bool CanLogin { get; set; } = false;

        [Display(Name = "Last Login Date")]
        public DateTime? LastLoginDate { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual UserInfo? User { get; set; }

        [ForeignKey("SpecialistId")]
        public virtual Specialist? Specialist { get; set; }

        public virtual ICollection<DoctorAssist> DoctorAssists { get; set; } = new List<DoctorAssist>();
        public virtual ICollection<Patient> Patients { get; set; } = new List<Patient>();
        public virtual ICollection<PatientDiagnosis> PatientDiagnoses { get; set; } = new List<PatientDiagnosis>();
    }
}
