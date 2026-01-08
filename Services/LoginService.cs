using ClinicManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

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
                    // Update last login
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
                    // Update last login
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

            // Authentication failed
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
