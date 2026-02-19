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
        public DbSet<DoctorReception> DoctorReceptions { get; set; }
        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<DoctorSubscription> DoctorSubscriptions { get; set; }

        public DbSet<Location> Locations { get; set; }
        public DbSet<AppointmentIntake> AppointmentIntakes { get; set; }
        public DbSet<IntakeQuestion> IntakeQuestions { get; set; }
        public DbSet<DoctorGroup> DoctorGroups { get; set; }
        public DbSet<DoctorGroupMember> DoctorGroupMembers { get; set; }
        public DbSet<PatientInvoice> PatientInvoices { get; set; }
        public DbSet<PatientInvoiceItem> PatientInvoiceItems { get; set; }
        /// <summary>
        /// Gets all doctor IDs that share a group with the given doctor (including the doctor themselves).
        /// If the doctor is not in any group, returns a list with only their own ID.
        /// </summary>
        public async Task<List<int>> GetGroupDoctorIdsAsync(int doctorId)
        {
            var groupIds = await DoctorGroupMembers
                .Where(m => m.DoctorId == doctorId && m.DoctorGroup!.Active)
                .Select(m => m.DoctorGroupId)
                .ToListAsync();

            if (!groupIds.Any())
                return new List<int> { doctorId };

            return await DoctorGroupMembers
                .Where(m => groupIds.Contains(m.DoctorGroupId))
                .Select(m => m.DoctorId)
                .Distinct()
                .ToListAsync();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships and constraints

            // Role -> UserInfo (One-to-Many)
            modelBuilder.Entity<UserInfo>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.SetNull);

            // UserInfo -> DoctorInfo (One-to-Many)
            modelBuilder.Entity<DoctorInfo>()
                .HasOne(d => d.User)
                .WithMany(u => u.Doctors)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Department -> Specialist (One-to-Many)
            modelBuilder.Entity<Specialist>()
                .HasOne(s => s.Department)
                .WithMany(d => d.Specialists)
                .HasForeignKey(s => s.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Specialist -> DoctorInfo (One-to-Many)
            modelBuilder.Entity<DoctorInfo>()
                .HasOne(d => d.Specialist)
                .WithMany(s => s.Doctors)
                .HasForeignKey(d => d.SpecialistId)
                .OnDelete(DeleteBehavior.SetNull);

            // DoctorInfo -> DoctorAssist (One-to-Many)
            modelBuilder.Entity<DoctorAssist>()
                .HasOne(da => da.Doctor)
                .WithMany(d => d.DoctorAssists)
                .HasForeignKey(da => da.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorInfo -> DoctorReception (One-to-Many)
            modelBuilder.Entity<DoctorReception>()
                .HasOne(dr => dr.Doctor)
                .WithMany(d => d.DoctorReceptions)
                .HasForeignKey(dr => dr.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorInfo -> Patient (One-to-Many)
            modelBuilder.Entity<Patient>()
                .HasOne(p => p.Doctor)
                .WithMany(d => d.Patients)
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Patient -> PatientDiagnosis (One-to-Many)
            modelBuilder.Entity<PatientDiagnosis>()
                .HasOne(pd => pd.Patient)
                .WithMany(p => p.PatientDiagnoses)
                .HasForeignKey(pd => pd.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorInfo -> PatientDiagnosis (One-to-Many)
            modelBuilder.Entity<PatientDiagnosis>()
                .HasOne(pd => pd.Doctor)
                .WithMany(d => d.PatientDiagnoses)
                .HasForeignKey(pd => pd.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);

            // DoctorInfo -> DoctorSubscription (One-to-Many)
            modelBuilder.Entity<DoctorSubscription>()
                .HasOne(ds => ds.Doctor)
                .WithMany(d => d.Subscriptions)
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Appointment -> AppointmentIntake (One-to-One)
            modelBuilder.Entity<AppointmentIntake>()
                .HasOne(ai => ai.Appointment)
                .WithOne(a => a.Intake)
                .HasForeignKey<AppointmentIntake>(ai => ai.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Specialist -> IntakeQuestion (One-to-Many)
            modelBuilder.Entity<IntakeQuestion>()
                .HasOne(iq => iq.Specialist)
                .WithMany()
                .HasForeignKey(iq => iq.SpecialistId)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorGroup -> DoctorGroupMember (One-to-Many)
            modelBuilder.Entity<DoctorGroupMember>()
                .HasOne(m => m.DoctorGroup)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.DoctorGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            // DoctorInfo -> DoctorGroupMember (One-to-Many)
            modelBuilder.Entity<DoctorGroupMember>()
                .HasOne(m => m.Doctor)
                .WithMany(d => d.GroupMemberships)
                .HasForeignKey(m => m.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // Prevent duplicate doctor in same group
            modelBuilder.Entity<DoctorGroupMember>()
                .HasIndex(m => new { m.DoctorGroupId, m.DoctorId })
                .IsUnique();

            // Patient -> PatientInvoice (One-to-Many)
            modelBuilder.Entity<PatientInvoice>()
                .HasOne(pi => pi.Patient)
                .WithMany()
                .HasForeignKey(pi => pi.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // DoctorInfo -> PatientInvoice (One-to-Many)
            modelBuilder.Entity<PatientInvoice>()
                .HasOne(pi => pi.Doctor)
                .WithMany()
                .HasForeignKey(pi => pi.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // Appointment -> PatientInvoice (One-to-Many, optional)
            modelBuilder.Entity<PatientInvoice>()
                .HasOne(pi => pi.Appointment)
                .WithMany()
                .HasForeignKey(pi => pi.AppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // PatientInvoice -> PatientInvoiceItem (One-to-Many)
            modelBuilder.Entity<PatientInvoiceItem>()
                .HasOne(pii => pii.Invoice)
                .WithMany(pi => pi.Items)
                .HasForeignKey(pii => pii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraints
            modelBuilder.Entity<DoctorInfo>()
                .HasIndex(d => d.LoginUsername)
                .IsUnique()
                .HasFilter("[LoginUsername] IS NOT NULL");

            modelBuilder.Entity<DoctorAssist>()
                .HasIndex(a => a.LoginUsername)
                .IsUnique()
                .HasFilter("[LoginUsername] IS NOT NULL");

            modelBuilder.Entity<DoctorReception>()
                .HasIndex(r => r.LoginUsername)
                .IsUnique()
                .HasFilter("[LoginUsername] IS NOT NULL");

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
                    UserPassword = "Admin@123", // Plain text - CHANGE THIS IN PRODUCTION!
                    UserFullName = "System Administrator",
                    JobTitle = "Administrator",
                    RoleId = 1, // Super Admin
                    Active = true
                }
            );
        }
    }
}