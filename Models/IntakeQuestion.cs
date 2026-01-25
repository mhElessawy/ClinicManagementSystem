using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class IntakeQuestion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Specialty")]
        public int SpecialistId { get; set; }

        [ForeignKey("SpecialistId")]
        public virtual Specialist? Specialist { get; set; }

        [Required]
        [Display(Name = "Question (Arabic)")]
        [StringLength(500)]
        public string QuestionAr { get; set; } = string.Empty;

        [Display(Name = "Question (English)")]
        [StringLength(500)]
        public string? QuestionEn { get; set; }

        [Required]
        [Display(Name = "Question Type")]
        [StringLength(50)]
        public string QuestionType { get; set; } = "YesNo"; // YesNo, Text, MultipleChoice, Scale

        // For MultipleChoice questions - comma separated options
        [Display(Name = "Options")]
        [StringLength(500)]
        public string? Options { get; set; }

        [Display(Name = "Display Order")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Is Required")]
        public bool IsRequired { get; set; } = false;

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;
    }
}
