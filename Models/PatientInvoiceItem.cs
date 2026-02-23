using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class PatientInvoiceItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Invoice")]
        public int InvoiceId { get; set; }

        [ForeignKey("InvoiceId")]
        public virtual PatientInvoice? Invoice { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Service Name")]
        public string ServiceName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Display(Name = "Unit Price")]
        [Column(TypeName = "decimal(10,3)")]
        public decimal UnitPrice { get; set; }

        [NotMapped]
        public decimal TotalPrice => Quantity * UnitPrice;
    }
}