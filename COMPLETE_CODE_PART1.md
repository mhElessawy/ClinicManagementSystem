# 🚀 Complete Clinic Management System V2 - All Code

## 📁 Project Structure
```
ClinicManagementSystem/
├── Models/
│   ├── Role.cs
│   ├── UserInfo.cs
│   ├── DoctorInfo.cs
│   ├── DoctorAssist.cs
│   ├── Patient.cs
│   ├── PatientDiagnosis.cs
│   ├── Department.cs
│   └── Specialist.cs
├── Data/
│   └── ApplicationDbContext.cs
├── Controllers/
│   ├── AccountController.cs
│   ├── HomeController.cs
│   ├── PatientsController.cs
│   └── [Others...]
├── Helpers/
│   └── SessionHelper.cs
├── Services/
│   └── LoginService.cs
└── Views/
    └── [46 views...]
```

---

## 📋 MODELS

### Role.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class Role
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Role Name")]
        public string RoleName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Permissions
        [Display(Name = "Can Manage Departments")]
        public bool CanManageDepartments { get; set; }

        [Display(Name = "Can Manage Specialists")]
        public bool CanManageSpecialists { get; set; }

        [Display(Name = "Can Manage Doctors")]
        public bool CanManageDoctors { get; set; }

        [Display(Name = "Can Manage Patients")]
        public bool CanManagePatients { get; set; }

        [Display(Name = "Can Manage Diagnoses")]
        public bool CanManageDiagnoses { get; set; }

        [Display(Name = "Can Manage Users")]
        public bool CanManageUsers { get; set; }

        [Display(Name = "Can View Reports")]
        public bool CanViewReports { get; set; }

        [Display(Name = "Can Manage Assistants")]
        public bool CanManageAssistants { get; set; }

        [Display(Name = "View All Patients")]
        public bool ViewAllPatients { get; set; }

        [Display(Name = "View Own Patients Only")]
        public bool ViewOwnPatientsOnly { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // Navigation properties
        public virtual ICollection<UserInfo> Users { get; set; } = new List<UserInfo>();
    }
}
```

### UserInfo.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class UserInfo
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50)]
        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string UserPassword { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string? UserFullName { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone")]
        public string? UserTel { get; set; }

        [StringLength(50)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [Display(Name = "Role")]
        public int? RoleId { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // Navigation Properties
        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }

        public virtual ICollection<DoctorInfo> Doctors { get; set; } = new List<DoctorInfo>();
    }
}
```

### DoctorInfo.cs
```csharp
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
```

### DoctorAssist.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class DoctorAssist
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Doctor is required")]
        [Display(Name = "Doctor")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "Assistant name is required")]
        [StringLength(100)]
        [Display(Name = "Assistant Name")]
        public string AssistName { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Phone 1")]
        public string? AssistTel1 { get; set; }

        [StringLength(20)]
        [Display(Name = "Phone 2")]
        public string? AssistTel2 { get; set; }

        [StringLength(200)]
        [Display(Name = "Address")]
        public string? AssistAddress { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

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
        [ForeignKey("DoctorId")]
        public virtual DoctorInfo Doctor { get; set; } = null!;
    }
}
```

### Patient.cs
```csharp
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
    }
}
```

### PatientDiagnosis.cs
```csharp
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

        [Required(ErrorMessage = "Diagnosis date is required")]
        [Display(Name = "Diagnosis Date")]
        public DateTime DiagnosisDate { get; set; } = DateTime.Now;

        [StringLength(1000)]
        [Display(Name = "Diagnosis Details")]
        public string? DiagnosisDetails { get; set; }

        [Display(Name = "Diagnosis File")]
        public byte[]? DiagnosisFile { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // Navigation Properties
        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; } = null!;

        [ForeignKey("DoctorId")]
        public virtual DoctorInfo? Doctor { get; set; }
    }
}
```

### Department.cs
```csharp
using System.ComponentModel.DataAnnotations;

namespace ClinicManagementSystem.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Department name is required")]
        [StringLength(100)]
        [Display(Name = "Department Name")]
        public string DepartmentName { get; set; } = string.Empty;

        // Navigation Properties
        public virtual ICollection<Specialist> Specialists { get; set; } = new List<Specialist>();
    }
}
```

### Specialist.cs
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManagementSystem.Models
{
    public class Specialist
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Specialist name is required")]
        [StringLength(100)]
        [Display(Name = "Specialist Name")]
        public string SpecialistName { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        // Navigation Properties
        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;

        public virtual ICollection<DoctorInfo> Doctors { get; set; } = new List<DoctorInfo>();
    }
}
```

---

## 📁 DATA LAYER

### ApplicationDbContext.cs (COMPLETE - Part 1/2)
```csharp
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles { get; set; }
        public DbSet<UserInfo> UserInfos { get; set; }
        public DbSet<DoctorInfo> DoctorInfos { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientDiagnosis> PatientDiagnoses { get; set; }
        public DbSet<Specialist> Specialists { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<DoctorAssist> DoctorAssists { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            
            // Role -> UserInfo
            modelBuilder.Entity<UserInfo>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            // UserInfo -> DoctorInfo
            modelBuilder.Entity<DoctorInfo>()
                .HasOne(d => d.User)
                .WithMany(u => u.Doctors)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Department -> Specialist
            modelBuilder.Entity<Specialist>()
                .HasOne(s => s.Department)
                .WithMany(d => d.Specialists)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Specialist -> DoctorInfo
            modelBuilder.Entity<DoctorInfo>()
                .HasOne(d => d.Specialist)
                .WithMany(s => s.Doctors)
                .HasForeignKey(d => d.SpecialistId)
                .OnDelete(DeleteBehavior.SetNull);

            // DoctorInfo -> DoctorAssist
            modelBuilder.Entity<DoctorAssist>()
                .HasOne(da => da.Doctor)
                .WithMany(d => d.DoctorAssists)
                .HasForeignKey(da => da.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorInfo -> Patient
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Doctor)
                .WithMany(d => d.Patients)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Patient -> PatientDiagnosis
            modelBuilder.Entity<PatientDiagnosis>()
                .HasOne(pd => pd.Patient)
                .WithMany(p => p.PatientDiagnoses)
                .HasForeignKey(pd => pd.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorInfo -> PatientDiagnosis
            modelBuilder.Entity<PatientDiagnosis>()
                .HasOne(pd => pd.Doctor)
                .WithMany(d => d.PatientDiagnoses)
                .HasForeignKey(pd => pd.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraints
            modelBuilder.Entity<DoctorInfo>()
                .HasIndex(d => d.LoginUsername)
                .IsUnique()
                .HasFilter("[LoginUsername] IS NOT NULL");

            modelBuilder.Entity<DoctorAssist>()
                .HasIndex(a => a.LoginUsername)
                .IsUnique()
                .HasFilter("[LoginUsername] IS NOT NULL");
```

### ApplicationDbContext.cs (Part 2/2 - Seed Data)
```csharp
            // Seed initial data
            
            // Roles
            modelBuilder.Entity<Role>().HasData(
                new Role 
                { 
                    Id = 1, 
                    RoleName = "Super Admin", 
                    Description = "Full system access",
                    CanManageDepartments = true,
                    CanManageSpecialists = true,
                    CanManageDoctors = true,
                    CanManagePatients = true,
                    CanManageDiagnoses = true,
                    CanManageUsers = true,
                    CanViewReports = true,
                    CanManageAssistants = true,
                    ViewAllPatients = true,
                    ViewOwnPatientsOnly = false,
                    Active = true
                },
                new Role 
                { 
                    Id = 2, 
                    RoleName = "Admin", 
                    Description = "Administrative access",
                    CanManageDepartments = true,
                    CanManageSpecialists = true,
                    CanManageDoctors = true,
                    CanManagePatients = true,
                    CanManageDiagnoses = false,
                    CanManageUsers = false,
                    CanViewReports = true,
                    CanManageAssistants = true,
                    ViewAllPatients = true,
                    ViewOwnPatientsOnly = false,
                    Active = true
                },
                new Role 
                { 
                    Id = 3, 
                    RoleName = "Doctor", 
                    Description = "Doctor access - own patients only",
                    CanManageDepartments = false,
                    CanManageSpecialists = false,
                    CanManageDoctors = false,
                    CanManagePatients = true,
                    CanManageDiagnoses = true,
                    CanManageUsers = false,
                    CanViewReports = false,
                    CanManageAssistants = false,
                    ViewAllPatients = false,
                    ViewOwnPatientsOnly = true,
                    Active = true
                },
                new Role 
                { 
                    Id = 4, 
                    RoleName = "Assistant", 
                    Description = "Doctor assistant - limited access",
                    CanManageDepartments = false,
                    CanManageSpecialists = false,
                    CanManageDoctors = false,
                    CanManagePatients = true,
                    CanManageDiagnoses = false,
                    CanManageUsers = false,
                    CanViewReports = false,
                    CanManageAssistants = false,
                    ViewAllPatients = false,
                    ViewOwnPatientsOnly = true,
                    Active = true
                },
                new Role 
                { 
                    Id = 5, 
                    RoleName = "Receptionist", 
                    Description = "Front desk - patient management only",
                    CanManageDepartments = false,
                    CanManageSpecialists = false,
                    CanManageDoctors = false,
                    CanManagePatients = true,
                    CanManageDiagnoses = false,
                    CanManageUsers = false,
                    CanViewReports = false,
                    CanManageAssistants = false,
                    ViewAllPatients = true,
                    ViewOwnPatientsOnly = false,
                    Active = true
                }
            );

            // Departments
            modelBuilder.Entity<Department>().HasData(
                new Department { Id = 1, DepartmentName = "Internal Medicine" },
                new Department { Id = 2, DepartmentName = "Surgery" },
                new Department { Id = 3, DepartmentName = "Pediatrics" },
                new Department { Id = 4, DepartmentName = "Orthopedics" },
                new Department { Id = 5, DepartmentName = "Cardiology" }
            );

            // Specialists
            modelBuilder.Entity<Specialist>().HasData(
                new Specialist { Id = 1, DepartmentId = 1, SpecialistName = "Cardiology", Description = "Heart and cardiovascular diseases" },
                new Specialist { Id = 2, DepartmentId = 2, SpecialistName = "General Surgery", Description = "General surgical procedures" },
                new Specialist { Id = 3, DepartmentId = 3, SpecialistName = "Pediatrics", Description = "Children's health care" },
                new Specialist { Id = 4, DepartmentId = 4, SpecialistName = "Orthopedic Surgery", Description = "Bone and joint surgery" },
                new Specialist { Id = 5, DepartmentId = 5, SpecialistName = "Cardiovascular", Description = "Heart and blood vessel diseases" }
            );

            // Default Super Admin User
            // TEMPORARY: Plain text password for testing
            // Login: admin / Admin@123
            modelBuilder.Entity<UserInfo>().HasData(
                new UserInfo 
                { 
                    Id = 1, 
                    UserName = "admin", 
                    UserPassword = "Admin@123", // Plain text - CHANGE IN PRODUCTION!
                    UserFullName = "System Administrator",
                    JobTitle = "Administrator",
                    RoleId = 1, // Super Admin
                    Active = true
                }
            );
        }
    }
}
```

---

## 🔧 HELPERS & SERVICES

### SessionHelper.cs
```csharp
using Microsoft.AspNetCore.Http;

namespace ClinicManagementSystem.Helpers
{
    public static class SessionHelper
    {
        // Session Keys
        private const string USER_ID = "UserId";
        private const string USER_NAME = "UserName";
        private const string USER_TYPE = "UserType";
        private const string DOCTOR_ID = "DoctorId";
        private const string ROLE_ID = "RoleId";
        private const string FULL_NAME = "FullName";

        // User Types
        public const string TYPE_ADMIN = "Admin";
        public const string TYPE_DOCTOR = "Doctor";
        public const string TYPE_ASSISTANT = "Assistant";

        // Set Session
        public static void SetUserSession(ISession session, int userId, string userName, 
            string userType, string fullName, int? doctorId = null, int? roleId = null)
        {
            session.SetInt32(USER_ID, userId);
            session.SetString(USER_NAME, userName);
            session.SetString(USER_TYPE, userType);
            session.SetString(FULL_NAME, fullName);
            
            if (doctorId.HasValue)
                session.SetInt32(DOCTOR_ID, doctorId.Value);
            
            if (roleId.HasValue)
                session.SetInt32(ROLE_ID, roleId.Value);
        }

        // Get Session Values
        public static int? GetUserId(ISession session)
        {
            return session.GetInt32(USER_ID);
        }

        public static string? GetUserName(ISession session)
        {
            return session.GetString(USER_NAME);
        }

        public static string? GetUserType(ISession session)
        {
            return session.GetString(USER_TYPE);
        }

        public static string? GetFullName(ISession session)
        {
            return session.GetString(FULL_NAME);
        }

        public static int? GetDoctorId(ISession session)
        {
            return session.GetInt32(DOCTOR_ID);
        }

        public static int? GetRoleId(ISession session)
        {
            return session.GetInt32(ROLE_ID);
        }

        // Check User Type
        public static bool IsAdmin(ISession session)
        {
            return GetUserType(session) == TYPE_ADMIN;
        }

        public static bool IsDoctor(ISession session)
        {
            return GetUserType(session) == TYPE_DOCTOR;
        }

        public static bool IsAssistant(ISession session)
        {
            return GetUserType(session) == TYPE_ASSISTANT;
        }

        // Check if logged in
        public static bool IsLoggedIn(ISession session)
        {
            return GetUserId(session).HasValue;
        }

        // Clear Session
        public static void ClearSession(ISession session)
        {
            session.Clear();
        }
    }
}
```

### LoginService.cs
```csharp
using ClinicManagementSystem.Data;
using ClinicManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace ClinicManagementSystem.Services
{
    public class LoginService
    {
        private readonly ApplicationDbContext _context;

        public LoginService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LoginResult> AuthenticateAsync(string username, string password)
        {
            // Try UserInfo (Admin/Staff)
            var user = await _context.UserInfos
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username && u.Active);

            if (user != null)
            {
                // TEMPORARY: Accept both BCrypt and plain text for testing
                bool isValid = false;
                
                try
                {
                    // Try BCrypt first
                    isValid = BCrypt.Net.BCrypt.Verify(password, user.UserPassword);
                }
                catch
                {
                    // If BCrypt fails, try plain text comparison (TEMPORARY)
                    isValid = user.UserPassword == password;
                }
                
                if (isValid)
                {
                    return new LoginResult
                    {
                        Success = true,
                        UserId = user.Id,
                        UserName = user.UserName,
                        FullName = user.UserFullName ?? user.UserName,
                        UserType = Helpers.SessionHelper.TYPE_ADMIN,
                        RoleId = user.RoleId
                    };
                }
            }

            // Try DoctorInfo
            var doctor = await _context.DoctorInfos
                .FirstOrDefaultAsync(d => d.LoginUsername == username && d.CanLogin && d.Active);

            if (doctor != null && !string.IsNullOrEmpty(doctor.LoginPassword))
            {
                bool isValid = false;
                
                try
                {
                    isValid = BCrypt.Net.BCrypt.Verify(password, doctor.LoginPassword);
                }
                catch
                {
                    isValid = doctor.LoginPassword == password;
                }
                
                if (isValid)
                {
                    doctor.LastLoginDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return new LoginResult
                    {
                        Success = true,
                        UserId = doctor.Id,
                        UserName = doctor.LoginUsername!,
                        FullName = doctor.DoctorName,
                        UserType = Helpers.SessionHelper.TYPE_DOCTOR,
                        DoctorId = doctor.Id
                    };
                }
            }

            // Try DoctorAssist
            var assistant = await _context.DoctorAssists
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.LoginUsername == username && a.CanLogin && a.Active);

            if (assistant != null && !string.IsNullOrEmpty(assistant.LoginPassword))
            {
                bool isValid = false;
                
                try
                {
                    isValid = BCrypt.Net.BCrypt.Verify(password, assistant.LoginPassword);
                }
                catch
                {
                    isValid = assistant.LoginPassword == password;
                }
                
                if (isValid)
                {
                    assistant.LastLoginDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return new LoginResult
                    {
                        Success = true,
                        UserId = assistant.Id,
                        UserName = assistant.LoginUsername!,
                        FullName = assistant.AssistName,
                        UserType = Helpers.SessionHelper.TYPE_ASSISTANT,
                        DoctorId = assistant.DoctorId
                    };
                }
            }

            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid username or password"
            };
        }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string? FullName { get; set; }
        public string? UserType { get; set; }
        public int? DoctorId { get; set; }
        public int? RoleId { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
```

---

Continued in next message...
