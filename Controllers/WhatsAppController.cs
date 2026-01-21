using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ClinicManagementSystem.Controllers
{
    [Authorize]
    public class WhatsAppController : Controller
    {
        public IActionResult SendMessage()
        {
            return View();
        }
    }
}
