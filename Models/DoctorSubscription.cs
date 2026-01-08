using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class DoctorSubscription
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Doctor is required")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        [Display(Name = "Subscription Type")]
        public string? SubscriptionType { get; set; } // Monthly, Yearly, etc.

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        // Navigation Properties
        [ForeignKey("DoctorId")]
        public virtual DoctorInfo Doctor { get; set; } = null!;

        // Computed property to check if subscription is currently valid
        [NotMapped]
        public bool IsCurrentlyValid
        {
            get
            {
                var today = DateTime.Today;
                return IsActive && StartDate <= today && EndDate >= today;
            }
        }

        // Computed property to check if subscription has expired
        [NotMapped]
        public bool IsExpired
        {
            get
            {
                return DateTime.Today > EndDate;
            }
        }

        // Computed property to get remaining days
        [NotMapped]
        public int RemainingDays
        {
            get
            {
                if (IsExpired) return 0;
                return (EndDate - DateTime.Today).Days;
            }
        }
    }
}
