using Microsoft.AspNetCore.Mvc;
using Patient_Appointment_Management_System.ViewModels; // Make sure this using is present
using System.Diagnostics;
using System.Collections.Generic; // For List
using System.Linq; // For Linq operations
using System; // For StringComparison

namespace Patient_Appointment_Management_System.Controllers
{
    public class AdminController : Controller
    {
        // Simulated data store for Admins (replace with database later)
        private static List<AdminUserViewModel> _admins = new List<AdminUserViewModel>
        {
            new AdminUserViewModel { AdminId = 1, Name = "Alice Admin", Email = "alice@system.com", Role = "Administrator" },
            new AdminUserViewModel { AdminId = 2, Name = "Bob SuperAdmin", Email = "bob@system.com", Role = "Super Admin" }
        };
        private static int _nextAdminId = 3;

        // Simulated data store for System Logs (replace with database or logging framework)
        private static List<string> _systemLogs = new List<string>
        {
            $"{DateTime.Now}: System initialized.",
            $"{DateTime.Now}: Admin Alice Admin (ID: 1) logged in.", // Example log
        };


        [HttpGet]
        public IActionResult AdminLogin()
        {
            // Clear any existing admin session on login page visit (optional)
            // HttpContext.Session.Clear(); // If using session for admin
            return View("~/Views/Home/AdminLogin.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Good practice for POST actions
        public IActionResult AdminLogin(string email, string password)
        {
            Debug.WriteLine($"Admin Login Attempt - Email: {email}"); // Avoid logging passwords

            if (!string.IsNullOrEmpty(email) && email.Equals("admin@example.com", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(password) && password.Equals("password123")) // Hardcoded credentials
            {
                // Simulate setting a session or cookie for admin
                // HttpContext.Session.SetString("AdminLoggedIn", "true");
                // HttpContext.Session.SetString("AdminEmail", email);
                TempData["AdminSuccessMessage"] = "Admin login successful!"; // Changed TempData key for clarity
                return RedirectToAction("AdminDashboard");
            }
            else
            {
                ViewBag.ErrorMessage = "Invalid email or password.";
                return View("~/Views/Home/AdminLogin.cshtml");
            }
        }

        [HttpGet]
        public IActionResult AdminDashboard()
        {
            // Basic check if admin is "logged in" (simulated)
            // if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            // {
            //    TempData["ErrorMessage"] = "Please log in as admin.";
            //    return RedirectToAction("AdminLogin");
            // }

            if (TempData["AdminSuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["AdminSuccessMessage"];
            }
            // Add any data needed for the dashboard view model here
            return View("~/Views/Home/AdminDashboard.cshtml");
        }

        // NEW ACTION for Admin Management page
        [HttpGet]
        public IActionResult AdminManagement()
        {
            // Basic check if admin is "logged in" (simulated)
            // if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            // {
            //    TempData["ErrorMessage"] = "Please log in as admin.";
            //    return RedirectToAction("AdminLogin");
            // }

            var viewModel = new AdminManagementViewModel
            {
                Admins = _admins.OrderBy(a => a.AdminId).ToList(),
                SystemLogs = _systemLogs.ToList() // Or take a subset, order by date, etc.
            };
            return View("~/Views/Home/AdminManagement.cshtml", viewModel);
        }

        // TODO: Add POST action for adding an admin (server-side)
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAdmin(AddAdminViewModel model)
        {
            // if (HttpContext.Session.GetString("AdminLoggedIn") != "true") return Unauthorized();

            if (ModelState.IsValid)
            {
                var newAdmin = new AdminUserViewModel
                {
                    AdminId = _nextAdminId++,
                    Name = model.Name,
                    Email = model.Email,
                    Role = model.Role
                };
                _admins.Add(newAdmin);
                _systemLogs.Add($"{DateTime.Now}: New admin '{model.Name}' added by current admin.");
                TempData["AdminManagementMessage"] = $"Admin '{model.Name}' added successfully.";
                return RedirectToAction("AdminManagement");
            }
            // If model state is invalid, you might want to handle it differently,
            // possibly by returning to the AdminManagement page with errors,
            // but modals are tricky for server-side validation display without AJAX.
            TempData["AdminManagementError"] = "Failed to add admin. Please check the details.";
            return RedirectToAction("AdminManagement"); // Or pass the model back if you can show errors in modal
        }
        */

        // TODO: Add POST action for deleting an admin (server-side)
        // TODO: Add GET/POST actions for editing an admin (server-side)


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogout()
        {
            // HttpContext.Session.Clear(); // Clear admin session
            TempData["GlobalSuccessMessage"] = "You have been successfully logged out as Admin."; // Use a consistent key
            return RedirectToAction("Index", "Home");
        }

        // Inside Patient_Appointment_Management_System/Controllers/AdminController.cs

        // ... (your existing AdminLogin, AdminDashboard, etc. methods) ...

        [HttpGet]
        public IActionResult AdminForgotPassword()
        {
            return View("~/Views/Home/AdminForgotPassword.cshtml", new AdminForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminForgotPassword(AdminForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO:
                // 1. Check if the model.Email exists in your admin user database.
                // 2. If it exists:
                //    a. Generate a unique, secure password reset token.
                //    b. Store this token in the database, associated with the admin user, along with an expiry timestamp.
                //    c. Construct a password reset link (e.g., https://yourdomain.com/Admin/ResetPassword?token=yourtoken).
                //    d. Send an email to model.Email containing this link.
                // 3. If it doesn't exist, you might still show the same message to prevent email enumeration.

                // For demonstration purposes, we'll set a TempData message.
                // In a real application, you would NOT confirm if the email exists directly to the user for security reasons.
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";

                return RedirectToAction("AdminLogin"); // Redirect to the login page
            }

            // If ModelState is invalid, return the view with validation errors
            return View("~/Views/Home/AdminForgotPassword.cshtml", model);
        }

        // You will also need actions for ResetPassword (GET to show form, POST to update password) later
        // For example:
        // [HttpGet]
        // public IActionResult AdminResetPassword(string token)
        // {
        //     // 1. Validate the token (exists, not expired, matches a user)
        //     // 2. If valid, show a view with password and confirm password fields.
        //     //    Pass the token to the view (e.g., in a hidden field).
        //     // If invalid, show an error message or redirect.
        //     return View("~/Views/Home/AdminResetPassword.cshtml", new AdminResetPasswordViewModel { Token = token });
        // }

        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public IActionResult AdminResetPassword(AdminResetPasswordViewModel model)
        // {
        //     // 1. Validate ModelState.
        //     // 2. Re-validate the model.Token.
        //     // 3. If valid, update the user's password in the database.
        //     // 4. Invalidate the token.
        //     // 5. Redirect to login with a success message.
        //     // If invalid, return the view with errors.
        // }

        // ... (rest of your AdminController code, e.g., AdminLogout) ...
    }
}