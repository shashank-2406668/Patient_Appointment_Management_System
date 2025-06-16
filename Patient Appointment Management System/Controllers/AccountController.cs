using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Patient_Appointment_Management_System.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet] 
        public IActionResult AdminLogin()
        {
            return View(); 
        }

        [HttpPost] 
        public IActionResult AdminLogin(string email, string password) 
        {
            Debug.WriteLine($"Admin Login Attempt - Email: {email}, Password: {password}");

            

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                
                TempData["LoginMessage"] = $"Login attempt for {email} received. (Backend logic not fully implemented yet)";
                return RedirectToAction("Index", "Home"); 
            }
            else
            {
               
                ViewBag.ErrorMessage = "Email or password was empty in submission (this is a basic check).";
                return View(); 
            }
        }

    }
}
