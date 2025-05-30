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
        [HttpGet] // This means this action responds to a GET request (like typing a URL in browser)
        public IActionResult AdminLogin()
        {
            return View(); // This tells MVC to look for a view named "AdminLogin.cshtml"
        }

        [HttpPost] // This means this action responds to a POST request (from a form submission)
        public IActionResult AdminLogin(string email, string password) // Parameters match 'name' attributes in form
        {
            // For now, let's just see if we received the data
            Debug.WriteLine($"Admin Login Attempt - Email: {email}, Password: {password}");

            // In a real app, you would:
            // 1. Check email and password against the database.
            // 2. If valid, log the user in (create a session/cookie).
            // 3. Redirect to the admin dashboard.
            // 4. If invalid, show an error message on the login page.

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                // Simplistic "success" - redirect to home page for now
                // In a real scenario, this would be after successful DB check and login
                TempData["LoginMessage"] = $"Login attempt for {email} received. (Backend logic not fully implemented yet)";
                return RedirectToAction("Index", "Home"); // Redirects to HomeController's Index action
            }
            else
            {
                // If something went wrong or data is missing, show the login page again with an error.
                // We'll add proper error messages later. For now, just reshow the view.
                ViewBag.ErrorMessage = "Email or password was empty in submission (this is a basic check).";
                return View(); // Re-displays AdminLogin.cshtml
            }
        }

    }
}
