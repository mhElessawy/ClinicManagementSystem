using ClinicManagementSystem.Helpers;
using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

static class MigrationCheck
{
    public static async Task RunAsync()
    {
        // Dedicated disposable database; never read the application's connection string.
        var databaseName = "ClinicTenancyTest_" + Guid.NewGuid().ToString("N");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;")
            .Options;
        var http = new DefaultHttpContext { Session = new MemorySession() };
        SessionHelper.SetUserSession(http.Session, 1, "admin", "Admin", "Admin", roleId: 1);
        var accessor = new HttpContextAccessor { HttpContext = http };
        try
        {
            // The repository's original initial migration has an empty Up. Build an
            // equivalent pre-clinic schema first, then test the real upgrade migrations.
            await using (var legacy = new LegacySchemaContext(options, accessor))
            {
                await legacy.Database.EnsureCreatedAsync();
                var group = new DoctorGroup { GroupName = "Existing clinic" };
                var doctor = new DoctorInfo { DoctorName = "Existing doctor", DoctorCivilId = "123", DoctorTel1 = "123", Email = "legacy@example.test", SpecialistId = 1 };
                legacy.AddRange(group, doctor);
                await legacy.SaveChangesAsync();
                legacy.DoctorGroupMembers.Add(new DoctorGroupMember { DoctorGroupId = group.Id, DoctorId = doctor.Id });
                legacy.UserInfos.Add(new UserInfo { UserName = "legacy-manager", UserPassword = "legacy", RoleId = 6, GroupId = group.Id });
                legacy.Patients.Add(new Patient { PatientName = "Existing patient", PatientTel1 = "123", DoctorId = doctor.Id });
                await legacy.SaveChangesAsync();
                await legacy.Database.ExecuteSqlRawAsync("""
                    CREATE TABLE [__EFMigrationsHistory] ([MigrationId] nvarchar(150) NOT NULL PRIMARY KEY, [ProductVersion] nvarchar(32) NOT NULL);
                    INSERT INTO [__EFMigrationsHistory] VALUES ('20260224164031_Latest', '8.0.0'), ('20260301183425_UPdatePatientInvoice', '8.0.0');
                    """);
            }
            await using var db = new ApplicationDbContext(options, accessor);
            await db.Database.MigrateAsync();
            var clinic = await db.Clinics.SingleAsync();
            var manager = await db.UserInfos.SingleAsync(u => u.UserName == "legacy-manager");
            var patient = await db.Patients.SingleAsync();
            var doctorAfter = await db.DoctorInfos.SingleAsync();
            if (manager.ClinicId != clinic.Id || patient.ClinicId != clinic.Id || doctorAfter.ClinicId != clinic.Id)
                throw new Exception("Legacy ownership was not preserved.");
            if (db.Database.HasPendingModelChanges()) throw new Exception("Migration model is stale.");
            Console.WriteLine("PASS: SQL Server upgrade preserves legacy clinic, manager, doctor and patient ownership.");
        }
        finally
        {
            await using var cleanup = new ApplicationDbContext(options, accessor);
            await cleanup.Database.EnsureDeletedAsync();
        }
    }
}

sealed class LegacySchemaContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor accessor)
    : ApplicationDbContext(options, accessor)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        foreach (var type in builder.Model.GetEntityTypes().Where(e => typeof(IClinicOwned).IsAssignableFrom(e.ClrType)).ToList())
        {
            builder.Entity(type.ClrType).HasQueryFilter(null);
            builder.Entity(type.ClrType).Ignore("Clinic");
            builder.Entity(type.ClrType).Ignore("ClinicId");
        }
        builder.Ignore<Clinic>();
        builder.Entity<DoctorInfo>().HasIndex(d => d.LoginUsername).IsUnique().HasFilter("[LoginUsername] IS NOT NULL");
        builder.Entity<DoctorAssist>().HasIndex(d => d.LoginUsername).IsUnique().HasFilter("[LoginUsername] IS NOT NULL");
        builder.Entity<DoctorReception>().HasIndex(d => d.LoginUsername).IsUnique().HasFilter("[LoginUsername] IS NOT NULL");
        // The original snapshot includes invalid SET NULL on required columns and
        // multiple cascade paths. Existing SQL Server schemas necessarily restrict
        // those deletes; model a valid legacy schema rather than those invalid DDLs.
        foreach (var foreignKey in builder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
    }
}
