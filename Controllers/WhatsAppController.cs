using Microsoft.AspNetCore.Mvc;
using ClinicManagementSystem.Helpers;

namespace ClinicManagementSystem.Controllers
{
    public class WhatsAppController : Controller
    {
        public IActionResult SendMessage()
        {
            if (!SessionHelper.IsLoggedIn(HttpContext.Session))
                return RedirectToAction("Login", "Account");

            return View();
        }
    }
}
