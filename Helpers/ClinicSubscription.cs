using ClinicManagementSystem.Models;

namespace ClinicManagementSystem.Helpers;

public static class ClinicSubscription
{
    // Subscription dates include the entire selected day in Kuwait (UTC+03:00).
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
    public const string AccessDeniedMessage = "Your clinic is inactive or its subscription has expired. Please contact the administrator to renew.";

    public static string Status(Clinic clinic) => !clinic.Active ? "Disabled"
        : clinic.SubscriptionEndsOn == null ? "Awaiting subscription"
        : clinic.SubscriptionEndsOn < Today ? "Expired" : "Active";
}
