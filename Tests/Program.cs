using ClinicManagementSystem.Helpers;
using ClinicManagementSystem.Models;
using ClinicManagementSystem.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

if (args.Contains("--sqlserver-migrations"))
{
    await MigrationCheck.RunAsync();
    return;
}

await using var connection = new SqliteConnection("Data Source=:memory:");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
var http = new DefaultHttpContext { Session = new MemorySession() };
var accessor = new HttpContextAccessor { HttpContext = http };
ApplicationDbContext Open() => new(options, accessor);
void Check(bool value, string label)
{
    if (!value) throw new Exception("FAIL: " + label);
    Console.WriteLine("PASS: " + label);
}
void Login(int? clinicId, string type = SessionHelper.TYPE_CLINIC_MANAGER, int userId = 1, int? groupId = null)
{
    SessionHelper.SetUserSession(http.Session, userId, "test", type, "Test", roleId: type == "Admin" ? 1 : 6, groupId: groupId);
    if (clinicId.HasValue) http.Session.SetInt32("ClinicId", clinicId.Value);
}
async Task Denied(Func<Task> action, string label)
{
    try { await action(); }
    catch (UnauthorizedAccessException) { Check(true, label); return; }
    throw new Exception("FAIL: " + label);
}

await using (var db = Open()) await db.Database.EnsureCreatedAsync();
var clinics = new List<Clinic>();
var managers = new List<UserInfo>();
for (int i = 0; i < 2; i++)
{
    http.Session.Clear();
    await using var db = Open();
    var clinic = new Clinic { Name = $"Clinic {i}", Code = $"CL-TEST{i}", Phone = "123", Email = $"clinic{i}@example.test" };
    var group = new DoctorGroup { GroupName = clinic.Name, Clinic = clinic };
    var manager = new UserInfo { UserName = "same-manager", UserPassword = BCrypt.Net.BCrypt.HashPassword("Test12345"), RoleId = 6, Clinic = clinic, Group = group };
    await db.RegisterClinicAsync(clinic, group, manager);
    clinics.Add(clinic);
    managers.Add(manager);
}
var doctors = new List<DoctorInfo>();
var patients = new List<Patient>();
for (int i = 0; i < 2; i++)
{
    Login(clinics[i].Id);
    await using var db = Open();
    var doctor = new DoctorInfo { DoctorName = $"Doctor {i}", DoctorCivilId = $"ID{i}", DoctorTel1 = "123", Email = $"doctor{i}@example.test", SpecialistId = 1, LoginUsername = $"doctor{i}",
        LoginPassword = BCrypt.Net.BCrypt.HashPassword("Test12345"), Active = true, CanLogin = true };
    db.DoctorInfos.Add(doctor);
    await db.SaveChangesAsync();
    var patient = new Patient { PatientName = $"Patient {i}", DoctorId = doctor.Id };
    db.Patients.Add(patient);
    await db.SaveChangesAsync();
    doctors.Add(doctor); patients.Add(patient);
    Check(doctor.ClinicId == clinics[i].Id && patient.ClinicId == clinics[i].Id, "Server assigns clinic ownership");
}

Login(clinics[0].Id);
await using (var db = Open())
{
    Check(await db.Patients.CountAsync() == 1, "Clinic sees only its patients");
    Check(await db.Patients.FindAsync(patients[1].Id) == null, "Direct patient ID from another clinic is hidden");
    db.Patients.Add(new Patient { PatientName = "Unassigned" });
    await db.SaveChangesAsync();
}
foreach (var type in new[] { "Doctor", "Assistant", "Reception", "ClinicManager" })
{
    Login(clinics[0].Id, type);
    await using var db = Open();
    Check(await db.Patients.CountAsync() == 2, $"{type} sees all clinic patients including unassigned patients");
}
Login(clinics[0].Id);
await using (var db = Open())
{
    db.Patients.Update(new Patient { Id = patients[1].Id, PatientName = "Forged", ClinicId = clinics[0].Id });
    await Denied(() => db.SaveChangesAsync(), "Detached cross-clinic update is rejected");
}
await using (var db = Open())
{
    db.Patients.Add(new Patient { PatientName = "Forged FK", DoctorId = doctors[1].Id });
    await Denied(() => db.SaveChangesAsync(), "Cross-clinic doctor foreign key is rejected");
}
await using (var db = Open())
{
    db.UserInfos.Add(new UserInfo { UserName = "escalate", UserPassword = "irrelevant", RoleId = 1 });
    await Denied(() => db.SaveChangesAsync(), "Clinic cannot create a site administrator");
}
http.Session.Clear();
await using (var db = Open())
{
    Check(await db.Patients.CountAsync() == 0, "Anonymous database scope sees no patients");
    var login = new LoginService(db);
    Check((await login.AuthenticateAsync("same-manager", "Test12345", "ClinicManager", clinics[1].Code)).ClinicId == clinics[1].Id,
        "Clinic code selects the correct account with the same username");
    Check(!(await login.AuthenticateAsync("same-manager", "Test12345", "ClinicManager", null)).Success, "Clinic code is mandatory");
    Check(!(await login.AuthenticateAsync("doctor0", "Test12345", "Doctor", clinics[1].Code)).Success, "Wrong clinic code prevents doctor login");
    Check((await login.AuthenticateAsync("doctor0", "Test12345", "Doctor", clinics[0].Code)).Success, "Clinic doctor logs in without individual subscription");
}
Login(null, "Admin");
await using (var db = Open())
{
    Check(await db.Patients.CountAsync() == 3 && await db.Clinics.CountAsync() == 2, "Site administrator sees both clinics");
    var clinic = await db.Clinics.FindAsync(clinics[0].Id);
    clinic!.Active = false;
    await db.SaveChangesAsync();
    Check(!(await new LoginService(db).AuthenticateAsync("doctor0", "Test12345", "Doctor", clinic.Code)).Success, "Disabled clinic cannot log in");
}
Login(clinics[0].Id, userId: managers[0].Id, groupId: managers[0].GroupId);
await using (var db = Open())
{
    var route = new RouteData(); route.Values["controller"] = "Patients";
    var action = new ActionExecutingContext(new ActionContext(http, route, new ActionDescriptor()), new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
    bool ran = false;
    await new ClinicAccessFilter(db).OnActionExecutionAsync(action, () => { ran = true; return Task.FromResult(new ActionExecutedContext(action, new List<IFilterMetadata>(), new object())); });
    Check(!ran && action.Result is RedirectToActionResult && !SessionHelper.IsLoggedIn(http.Session), "Disabling clinic revokes an existing session");
}
Console.WriteLine("All clinic tenancy checks passed.");

sealed class MemorySession : ISession
{
    private readonly Dictionary<string, byte[]> data = new();
    public bool IsAvailable => true;
    public string Id => "test";
    public IEnumerable<string> Keys => data.Keys;
    public void Clear() => data.Clear();
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Remove(string key) => data.Remove(key);
    public void Set(string key, byte[] value) => data[key] = value;
    public bool TryGetValue(string key, out byte[] value) => data.TryGetValue(key, out value!);
}
