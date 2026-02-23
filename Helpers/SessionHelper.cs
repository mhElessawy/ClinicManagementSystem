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
        private const string GROUP_ID = "GroupId";

        // User Types
        public const string TYPE_ADMIN = "Admin";
        public const string TYPE_DOCTOR = "Doctor";
        public const string TYPE_ASSISTANT = "Assistant";
        public const string TYPE_RECEPTION = "Reception";
        public const string TYPE_CLINIC_MANAGER = "ClinicManager";

        // Set Session
        public static void SetUserSession(ISession session, int userId, string userName, string userType, string fullName, int? doctorId = null, int? roleId = null, int? groupId = null)
        {
            session.SetInt32(USER_ID, userId);
            session.SetString(USER_NAME, userName);
            session.SetString(USER_TYPE, userType);
            session.SetString(FULL_NAME, fullName);

            if (doctorId.HasValue)
                session.SetInt32(DOCTOR_ID, doctorId.Value);

            if (roleId.HasValue)
                session.SetInt32(ROLE_ID, roleId.Value);

            if (groupId.HasValue)
                session.SetInt32(GROUP_ID, groupId.Value);
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

        public static int? GetGroupId(ISession session)
        {
            return session.GetInt32(GROUP_ID);
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

        public static bool IsReception(ISession session)
        {
            return GetUserType(session) == TYPE_RECEPTION;
        }

        public static bool IsClinicManager(ISession session)
        {
            return GetUserType(session) == TYPE_CLINIC_MANAGER;
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