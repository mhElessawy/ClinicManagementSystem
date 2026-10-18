using ClinicManagementSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagementSystem.Helpers;

public sealed class ClinicAccessFilter(ApplicationDbContext db) : IAsyncActionFilter, IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is UnauthorizedAccessException)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            context.ExceptionHandled = true;
        }
    }

    private static readonly HashSet<string> SiteAdminControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Clinics", "UserInfos", "DoctorGroups", "Departments", "Specialists", "Reports",
        "DoctorInfos", "DoctorAssists", "DoctorSubscriptions"
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
        if (controller == "Account") { await next(); return; }
        var session = context.HttpContext.Session;
        if (!SessionHelper.IsLoggedIn(session))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }

        var userId = SessionHelper.GetUserId(session);
        var clinicId = session.GetInt32("ClinicId");
        var userType = SessionHelper.GetUserType(session);
        bool valid;
        if (SessionHelper.IsSuperAdmin(session))
            valid = await db.UserInfos.IgnoreQueryFilters().AnyAsync(u => u.Id == userId && u.Active && u.RoleId == 1 && u.ClinicId == null);
        else if (clinicId == null || !await db.Clinics.AnyAsync(c => c.Id == clinicId && c.Active))
            valid = false;
        else
            valid = userType switch
            {
                SessionHelper.TYPE_CLINIC_MANAGER => await db.UserInfos.AnyAsync(u => u.Id == userId && u.Active && u.RoleId == 6 && u.GroupId == SessionHelper.GetGroupId(session)),
                SessionHelper.TYPE_DOCTOR => await db.DoctorInfos.AnyAsync(u => u.Id == userId && u.Active && u.CanLogin),
                SessionHelper.TYPE_ASSISTANT => await db.DoctorAssists.AnyAsync(u => u.Id == userId && u.Active && u.CanLogin),
                SessionHelper.TYPE_RECEPTION => await db.DoctorReceptions.AnyAsync(u => u.Id == userId && u.Active && u.CanLogin),
                _ => false
            };
        if (!valid)
        {
            session.Clear();
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        if (SiteAdminControllers.Contains(controller) && !SessionHelper.IsSuperAdmin(session))
        {
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }
        if (SessionHelper.IsSuperAdmin(session) && HttpMethods.IsPost(context.HttpContext.Request.Method) &&
            context.RouteData.Values["action"]?.ToString() == "Create" &&
            controller is "DoctorInfos" or "DoctorGroups" or "UserInfos" or "Patients")
        {
            foreach (var owned in context.ActionArguments.Values.OfType<IClinicOwned>())
                if (owned.ClinicId == null && owned is not UserInfo { RoleId: 1 })
                    context.ModelState.AddModelError("ClinicId", "Select the clinic that owns this record.");
        }

        if (HttpMethods.IsPost(context.HttpContext.Request.Method) && controller == "ClinicManager")
        {
            // Feed credential errors into the existing forms and their dropdown population.
            foreach (var argument in context.ActionArguments.Values)
            {
                var credentials = argument switch
                {
                    DoctorInfo d => (d.Id, d.LoginUsername, d.LoginPassword, d.CanLogin, "Doctor"),
                    DoctorAssist a => (a.Id, a.LoginUsername, a.LoginPassword, a.CanLogin, "Assistant"),
                    DoctorReception r => (r.Id, r.LoginUsername, r.LoginPassword, r.CanLogin, "Reception"),
                    _ => default
                };
                var (id, username, password, canLogin, type) = credentials;
                if (type == null) continue;
                if (canLogin && string.IsNullOrWhiteSpace(username))
                    context.ModelState.AddModelError("LoginUsername", "A username is required to enable login.");
                if (canLogin && id == 0 && string.IsNullOrWhiteSpace(password))
                    context.ModelState.AddModelError("LoginPassword", "A password is required to enable login.");
                if (!string.IsNullOrEmpty(password) && (password.Length < 8 || password.Length > 72))
                    context.ModelState.AddModelError("LoginPassword", "The password must be between 8 and 72 characters long.");
                if (string.IsNullOrWhiteSpace(username)) continue;
                var duplicate = type switch
                {
                    "Doctor" => await db.DoctorInfos.AnyAsync(d => d.Id != id && d.LoginUsername == username),
                    "Assistant" => await db.DoctorAssists.AnyAsync(d => d.Id != id && d.LoginUsername == username),
                    _ => await db.DoctorReceptions.AnyAsync(d => d.Id != id && d.LoginUsername == username)
                };
                if (duplicate) context.ModelState.AddModelError("LoginUsername", "This username is already used in your clinic.");
            }
        }
        await next();
    }
}
