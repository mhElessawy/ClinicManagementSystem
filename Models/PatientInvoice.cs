using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class PatientInvoice
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Patient is required")]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [Display(Name = "Doctor")]
        public int? DoctorId { get; set; }

        [Required(ErrorMessage = "Invoice date is required")]
        [Display(Name = "Invoice Date")]
        public DateTime InvoiceDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Total amount is required")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Total Amount")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Paid Amount")]
        public decimal PaidAmount { get; set; } = 0;

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // Navigation Properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual DoctorInfo? Doctor { get; set; }
    }
}
