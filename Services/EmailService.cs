using ClinicManagementSystem.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Mail;

namespace ClinicManagementSystem.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task SendDoctorRegistrationNotificationAsync(DoctorInfo doctor);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink, string doctorName);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder();
                if (isHtml)
                    bodyBuilder.HtmlBody = body;
                else
                    bodyBuilder.TextBody = body;

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new MailKit.Net.Smtp.SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                throw;
            }
        }

        public async Task SendDoctorRegistrationNotificationAsync(DoctorInfo doctor)
        {
            var subject = "New Doctor Registration - Action Required";
            var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                    .info-row {{ padding: 10px 0; border-bottom: 1px solid #eee; }}
                    .label {{ font-weight: bold; color: #667eea; }}
                    .footer {{ background: #333; color: white; padding: 15px; text-align: center; border-radius: 0 0 10px 10px; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>New Doctor Registration</h2>
                    </div>
                    <div class='content'>
                        <p>A new doctor has registered on the system and is waiting for approval.</p>
 
                        <div class='info-row'>
                            <span class='label'>Doctor Name:</span> {doctor.DoctorName}
                        </div>
                        <div class='info-row'>
                            <span class='label'>Email:</span> {doctor.Email}
                        </div>
                        <div class='info-row'>
                            <span class='label'>Civil ID:</span> {doctor.DoctorCivilId}
                        </div>
                        <div class='info-row'>
                            <span class='label'>Phone:</span> {doctor.DoctorTel1}
                        </div>
                        <div class='info-row'>
                            <span class='label'>Username:</span> {doctor.LoginUsername}
                        </div>
                        <div class='info-row'>
                            <span class='label'>Registration Date:</span> {doctor.RegDate?.ToString("yyyy-MM-dd HH:mm")}
                        </div>
 
                        <p style='margin-top: 20px; padding: 15px; background: #fff3cd; border-radius: 5px;'>
                            <strong>Action Required:</strong> Please login to the admin panel to review and approve this registration.
                        </p>
                    </div>
                    <div class='footer'>
                        Doctor Management System - Automated Notification
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(_emailSettings.AdminEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink, string doctorName)
        {
            var subject = "Password Reset Request - Doctor Management System";
            var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #f9f9f9; padding: 30px; border: 1px solid #ddd; }}
                    .button {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
                    .footer {{ background: #333; color: white; padding: 15px; text-align: center; border-radius: 0 0 10px 10px; font-size: 12px; }}
                    .warning {{ background: #fff3cd; padding: 15px; border-radius: 5px; margin-top: 20px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Password Reset Request</h2>
                    </div>
                    <div class='content'>
                        <p>Hello <strong>{doctorName}</strong>,</p>
 
                        <p>We received a request to reset your password for your Doctor Management System account.</p>
 
                        <p>Click the button below to reset your password:</p>
 
                        <p style='text-align: center;'>
                            <a href='{resetLink}' class='button'>Reset Password</a>
                        </p>
 
                        <p>Or copy and paste this link into your browser:</p>
                        <p style='word-break: break-all; background: #eee; padding: 10px; border-radius: 5px;'>{resetLink}</p>
 
                        <div class='warning'>
                            <strong>Note:</strong> This link will expire in 1 hour for security reasons. If you didn't request a password reset, please ignore this email.
                        </div>
                    </div>
                    <div class='footer'>
                        Doctor Management System - Automated Notification
                    </div>
                </div>
            </body>
            </html>";

            await SendEmailAsync(toEmail, subject, body);
        }
    }
}
