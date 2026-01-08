using BCrypt.Net;
using System;

namespace ClinicManagementSystem.Tools
{
    /// <summary>
    /// Helper tool to generate BCrypt password hashes
    /// USE THIS TO CREATE HASHED PASSWORDS
    /// </summary>
    public class PasswordHashGenerator
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== BCrypt Password Hash Generator ===");
            Console.WriteLine();
            
            // Generate hash for Admin@123
            string adminPassword = "Admin@123";
            string adminHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            
            Console.WriteLine($"Password: {adminPassword}");
            Console.WriteLine($"Hash: {adminHash}");
            Console.WriteLine($"Verify: {BCrypt.Net.BCrypt.Verify(adminPassword, adminHash)}");
            Console.WriteLine();
            
            // Generate hash for Doctor123
            string doctorPassword = "Doctor123";
            string doctorHash = BCrypt.Net.BCrypt.HashPassword(doctorPassword);
            
            Console.WriteLine($"Password: {doctorPassword}");
            Console.WriteLine($"Hash: {doctorHash}");
            Console.WriteLine($"Verify: {BCrypt.Net.BCrypt.Verify(doctorPassword, doctorHash)}");
            Console.WriteLine();
            
            // Generate hash for Assistant123
            string assistPassword = "Assistant123";
            string assistHash = BCrypt.Net.BCrypt.HashPassword(assistPassword);
            
            Console.WriteLine($"Password: {assistPassword}");
            Console.WriteLine($"Hash: {assistHash}");
            Console.WriteLine($"Verify: {BCrypt.Net.BCrypt.Verify(assistPassword, assistHash)}");
            Console.WriteLine();
            
            Console.WriteLine("=== SQL Update Scripts ===");
            Console.WriteLine();
            Console.WriteLine($"-- Update Admin Password");
            Console.WriteLine($"UPDATE UserInfos SET UserPassword = '{adminHash}' WHERE UserName = 'admin';");
            Console.WriteLine();
            Console.WriteLine($"-- Update Doctor Password (example)");
            Console.WriteLine($"UPDATE DoctorInfos SET LoginPassword = '{doctorHash}' WHERE LoginUsername = 'dr.smith';");
            Console.WriteLine();
            Console.WriteLine($"-- Update Assistant Password (example)");
            Console.WriteLine($"UPDATE DoctorAssists SET LoginPassword = '{assistHash}' WHERE LoginUsername = 'assistant1';");
            Console.WriteLine();
            
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        
        // Method to generate hash programmatically
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
        
        // Method to verify password
        public static bool VerifyPassword(string password, string hash)
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}
