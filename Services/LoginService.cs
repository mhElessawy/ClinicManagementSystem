using ClinicManagementSystem.Models;
using ClinicManagementSystem.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Services;

public class LoginService
{
    private readonly ApplicationDbContext _context;
    public LoginService(ApplicationDbContext context) => _context = context;

    public async Task<LoginResult> AuthenticateAsync(string username, string password, string userType, string? clinicCode)
    {
        var failure = new LoginResult { ErrorMessage = "بيانات الدخول أو كود العيادة غير صحيح." };
        username = username.Trim();
        int? clinicId = null;
        if (userType != SessionHelper.TYPE_ADMIN)
        {
            var code = clinicCode?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(code)) return failure;
            clinicId = await _context.Clinics.Where(c => c.Code == code && c.Active)
                .Select(c => (int?)c.Id).SingleOrDefaultAsync();
            if (clinicId == null) return failure;
        }

        if (userType == SessionHelper.TYPE_ADMIN || userType == SessionHelper.TYPE_CLINIC_MANAGER)
        {
            var user = await _context.UserInfos.IgnoreQueryFilters().FirstOrDefaultAsync(u =>
                u.UserName == username && u.Active && u.ClinicId == clinicId &&
                (userType == SessionHelper.TYPE_ADMIN ? u.RoleId == 1 : u.RoleId == 6));
            if (user == null || !Verify(password, user.UserPassword)) return failure;
            return new LoginResult { Success = true, UserId = user.Id, UserName = user.UserName,
                FullName = user.UserFullName ?? user.UserName, UserType = userType,
                RoleId = user.RoleId, GroupId = user.GroupId, ClinicId = clinicId };
        }
        if (userType == SessionHelper.TYPE_DOCTOR)
        {
            var doctor = await _context.DoctorInfos.IgnoreQueryFilters().FirstOrDefaultAsync(d =>
                d.ClinicId == clinicId && d.LoginUsername == username && d.Active && d.CanLogin);
            if (doctor == null || !Verify(password, doctor.LoginPassword)) return failure;
            // The clinic account owns access; doctors no longer need individual subscriptions.
            return new LoginResult { Success = true, UserId = doctor.Id, UserName = doctor.LoginUsername,
                FullName = doctor.DoctorName, UserType = userType, DoctorId = doctor.Id, ClinicId = clinicId };
        }
        if (userType == SessionHelper.TYPE_ASSISTANT)
        {
            var assistant = await _context.DoctorAssists.IgnoreQueryFilters().FirstOrDefaultAsync(a =>
                a.ClinicId == clinicId && a.LoginUsername == username && a.Active && a.CanLogin);
            if (assistant == null || !Verify(password, assistant.LoginPassword)) return failure;
            return new LoginResult { Success = true, UserId = assistant.Id, UserName = assistant.LoginUsername,
                FullName = assistant.AssistName, UserType = userType, DoctorId = assistant.DoctorId, ClinicId = clinicId };
        }
        if (userType == SessionHelper.TYPE_RECEPTION)
        {
            var reception = await _context.DoctorReceptions.IgnoreQueryFilters().FirstOrDefaultAsync(r =>
                r.ClinicId == clinicId && r.LoginUsername == username && r.Active && r.CanLogin);
            if (reception == null || !Verify(password, reception.LoginPassword)) return failure;
            return new LoginResult { Success = true, UserId = reception.Id, UserName = reception.LoginUsername,
                FullName = reception.ReceptionName, UserType = userType, DoctorId = reception.DoctorId, ClinicId = clinicId };
        }
        return failure;
    }

    private static bool Verify(string password, string? hash)
    {
        if (string.IsNullOrEmpty(hash)) return false;
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch (BCrypt.Net.SaltParseException) { return hash == password; } // Existing accounts only.
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
    public int? GroupId { get; set; }
    public int? ClinicId { get; set; }
    public string? ErrorMessage { get; set; }
}
