// Controllers/HomeController.cs
using Microsoft.AspNetCore.Mvc;
using Patient_Appointment_Management_System.Models;
using System.Diagnostics;

namespace Patient_Appointment_Management_System.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            // This will naturally look for Views/Home/Index.cshtml
            // but to be explicit like other controllers with this structure:
            return View("~/Views/Home/Index.cshtml");
        }

        public IActionResult Privacy()
        {
            return View("~/Views/Home/Privacy.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            // Assuming you have an Error.cshtml in Views/Home/
            return View("~/Views/Home/Error.cshtml", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}