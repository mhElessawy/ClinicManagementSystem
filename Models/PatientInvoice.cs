using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class PatientInvoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Invoice Number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Patient")]
        public int PatientId { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient? Patient { get; set; }

        [Required]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [ForeignKey("DoctorId")]
        public virtual DoctorInfo? Doctor { get; set; }

        [Display(Name = "Appointment")]
        public int? AppointmentId { get; set; }

        [ForeignKey("AppointmentId")]
        public virtual Appointment? Appointment { get; set; }

        [Required]
        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Display(Name = "Discount")]
        [Column(TypeName = "decimal(10,3)")]
        public decimal Discount { get; set; } = 0;

        [Display(Name = "Paid Amount")]
        [Column(TypeName = "decimal(10,3)")]
        public decimal PaidAmount { get; set; } = 0;

        [Display(Name = "Payment Status")]
        [StringLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid"; // Paid, Partial, Unpaid

        [Display(Name = "Payment Method")]
        [StringLength(20)]
        public string? PaymentMethod { get; set; } // Cash, Card, Insurance

        [Display(Name = "Notes")]
        [StringLength(500)]
        public string? Notes { get; set; }

        [Display(Name = "Created By")]
        public int? CreatedBy { get; set; }

        [Display(Name = "Created By Type")]
        [StringLength(20)]
        public string CreatedByType { get; set; } = string.Empty;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation property for line items
        public virtual ICollection<PatientInvoiceItem> Items { get; set; } = new List<PatientInvoiceItem>();

        // Computed properties
        [NotMapped]
        public decimal TotalAmount => Items?.Sum(i => i.TotalPrice) ?? 0;

        [NotMapped]
        public decimal NetAmount => TotalAmount - Discount;

        [NotMapped]
        public decimal RemainingAmount => NetAmount - PaidAmount;
    }
}
