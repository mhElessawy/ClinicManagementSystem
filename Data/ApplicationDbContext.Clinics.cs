using ClinicManagementSystem.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Models;

public partial class ApplicationDbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private bool _registeringClinic;
    public DbSet<Clinic> Clinics { get; set; }
    public int? CurrentClinicId => _httpContextAccessor?.HttpContext?.Session.GetInt32("ClinicId");
    public bool IsSiteAdmin => _httpContextAccessor?.HttpContext is { } http &&
        SessionHelper.IsSuperAdmin(http.Session);

    private void ConfigureClinics(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Clinic>().HasIndex(c => c.Code).IsUnique();
        ConfigureClinicEntity<UserInfo>(modelBuilder);
        ConfigureClinicEntity<DoctorInfo>(modelBuilder);
        ConfigureClinicEntity<DoctorAssist>(modelBuilder);
        ConfigureClinicEntity<DoctorReception>(modelBuilder);
        ConfigureClinicEntity<Patient>(modelBuilder);
        ConfigureClinicEntity<DoctorGroup>(modelBuilder);
        ConfigureClinicEntity<DoctorGroupMember>(modelBuilder);
        ConfigureClinicEntity<Location>(modelBuilder);
        ConfigureClinicEntity<Appointment>(modelBuilder);
        ConfigureClinicEntity<AppointmentIntake>(modelBuilder);
        ConfigureClinicEntity<PatientDiagnosis>(modelBuilder);
        ConfigureClinicEntity<PatientInvoice>(modelBuilder);
        ConfigureClinicEntity<PatientInvoiceItem>(modelBuilder);
        ConfigureClinicEntity<DoctorSubscription>(modelBuilder);
        modelBuilder.Entity<UserInfo>().HasIndex(u => new { u.ClinicId, u.UserName }).IsUnique();
    }

    private void ConfigureClinicEntity<T>(ModelBuilder builder) where T : class, IClinicOwned
    {
        builder.Entity<T>().HasOne(e => e.Clinic).WithMany().HasForeignKey(e => e.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<T>().HasQueryFilter(e => IsSiteAdmin ||
            (CurrentClinicId != null && e.ClinicId == CurrentClinicId));
    }

    // Only constructed registration entities enter here; never accept entity instances from a form.
    public async Task RegisterClinicAsync(Clinic clinic, DoctorGroup group, UserInfo manager)
    {
        if (manager.RoleId != 6 || group.Clinic != clinic || manager.Clinic != clinic || manager.Group != group)
            throw new InvalidOperationException("Invalid clinic registration.");
        _registeringClinic = true;
        try
        {
            AddRange(clinic, group, manager);
            await SaveChangesAsync();
        }
        finally { _registeringClinic = false; }
    }

    private void ValidateClinicChanges()
    {
        ChangeTracker.DetectChanges();
        var changes = ChangeTracker.Entries().Where(e =>
            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        if (_registeringClinic) return;

        foreach (var entry in changes)
        {
            if (entry.Entity is Clinic && !IsSiteAdmin)
                throw new UnauthorizedAccessException("Only the site administrator can change clinic records.");
            if (entry.Entity is not IClinicOwned owned) continue;

            if (entry.State != EntityState.Added)
            {
                // Fetch the stored owner even for detached form models. A posted ClinicId is untrusted.
                var stored = entry.GetDatabaseValues();
                if (stored == null) throw new DbUpdateConcurrencyException();
                var storedClinicId = stored.GetValue<int?>(nameof(IClinicOwned.ClinicId));
                if (!IsSiteAdmin && (CurrentClinicId == null || storedClinicId != CurrentClinicId))
                    throw new UnauthorizedAccessException("Record belongs to another clinic.");
                owned.ClinicId = storedClinicId;
            }
            else if (!IsSiteAdmin)
            {
                if (CurrentClinicId == null) throw new UnauthorizedAccessException("A clinic is required.");
                owned.ClinicId = CurrentClinicId;
            }

            if (entry.State == EntityState.Added && IsSiteAdmin && owned.ClinicId == null)
            {
                foreach (var foreignKey in entry.Metadata.GetForeignKeys())
                {
                    if (!typeof(IClinicOwned).IsAssignableFrom(foreignKey.PrincipalEntityType.ClrType)) continue;
                    var keys = foreignKey.Properties.Select(p => entry.Property(p.Name).CurrentValue).ToArray();
                    if (keys.Any(k => k == null)) continue;
                    if (Find(foreignKey.PrincipalEntityType.ClrType, keys) is IClinicOwned { ClinicId: int parentClinicId })
                    {
                        owned.ClinicId = parentClinicId;
                        break;
                    }
                }
            }
            if (entry.State == EntityState.Added && owned.ClinicId == null && entry.Entity is not UserInfo { RoleId: 1 })
                throw new UnauthorizedAccessException("Select a clinic for this record.");

            if (entry.Entity is UserInfo user && !IsSiteAdmin)
                throw new UnauthorizedAccessException("Only the site administrator can manage system accounts.");

            // Validate every tenant-owned foreign key, including posted doctor/patient/group IDs.
            // This also covers child rows attached as an invoice graph.
            if (entry.State == EntityState.Deleted) continue;
            foreach (var foreignKey in entry.Metadata.GetForeignKeys())
            {
                if (!typeof(IClinicOwned).IsAssignableFrom(foreignKey.PrincipalEntityType.ClrType)) continue;
                var values = foreignKey.Properties.Select(p => entry.Property(p.Name).CurrentValue).ToArray();
                if (values.Any(v => v == null)) continue;
                var principal = Find(foreignKey.PrincipalEntityType.ClrType, values) as IClinicOwned;
                // New principals may still be using temporary identity keys.
                if (principal != null && Entry(principal).State == EntityState.Added && !IsSiteAdmin)
                    principal.ClinicId = CurrentClinicId;
                // Legacy DoctorInfo.UserId may identify the site admin who created the record.
                if (entry.Entity is DoctorInfo && principal is UserInfo { RoleId: 1, ClinicId: null }) continue;
                if (principal == null || principal.ClinicId != owned.ClinicId)
                    throw new UnauthorizedAccessException("Related records must belong to the same clinic.");
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateClinicChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateClinicChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
