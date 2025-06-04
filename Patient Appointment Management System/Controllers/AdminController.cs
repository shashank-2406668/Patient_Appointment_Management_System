using Microsoft.AspNetCore.Mvc;
using Patient_Appointment_Management_System.ViewModels; // For all your ViewModels
using Patient_Appointment_Management_System.Services;   // For IAdminService
using Patient_Appointment_Management_System.Models;     // For Admin model
using Patient_Appointment_Management_System.Utils;      // For PasswordHelper
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;                           // For async operations
// using Microsoft.AspNetCore.Authorization; // Uncomment if/when you implement ASP.NET Core Identity or custom auth schemes

namespace Patient_Appointment_Management_System.Controllers
{
    // [Authorize] // You might want to authorize the entire controller once auth is fully set up
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        // If you were to use a dedicated logging service like ILogger:
        // private readonly ILogger<AdminController> _logger;

        // public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
            // _logger = logger; // If using ILogger
        }

        // GET: /Admin/AdminLogin
        [HttpGet]
        public IActionResult AdminLogin()
        {
            // URGENT: Standard path would be ~/Views/Admin/AdminLogin.cshtml
            return View("~/Views/Home/AdminLogin.cshtml", new AdminLoginViewModel());
        }

        // POST: /Admin/AdminLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // URGENT: Standard path would be ~/Views/Admin/AdminLogin.cshtml
                return View("~/Views/Home/AdminLogin.cshtml", model);
            }

            // _logger?.LogInformation($"Admin login attempt for email: {model.Email}"); // Example with ILogger
            Debug.WriteLine($"Admin Login Attempt - Email: {model.Email}");

            var admin = await _adminService.GetAdminByEmailAsync(model.Email);

            if (admin != null && PasswordHelper.VerifyPassword(model.Password, admin.PasswordHash))
            {
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                HttpContext.Session.SetInt32("AdminId", admin.AdminId);
                HttpContext.Session.SetString("AdminName", admin.Name);
                HttpContext.Session.SetString("AdminRole", admin.Role ?? "Admin"); // Ensure role is not null for session

                TempData["AdminSuccessMessage"] = "Admin login successful!";
                // _logger?.LogInformation($"Admin login successful for email: {model.Email}, AdminId: {admin.AdminId}");
                return RedirectToAction("AdminDashboard");
            }
            else
            {
                // _logger?.LogWarning($"Admin login failed for email: {model.Email}");
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                // URGENT: Standard path would be ~/Views/Admin/AdminLogin.cshtml
                return View("~/Views/Home/AdminLogin.cshtml", model);
            }
        }

        // GET: /Admin/AdminDashboard
        [HttpGet]
        public IActionResult AdminDashboard()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                TempData["ErrorMessage"] = "Please log in as admin.";
                return RedirectToAction("AdminLogin");
            }

            if (TempData.ContainsKey("AdminSuccessMessage"))
            {
                ViewBag.SuccessMessage = TempData["AdminSuccessMessage"];
            }
            // URGENT: Standard path would be ~/Views/Admin/AdminDashboard.cshtml
            return View("~/Views/Home/AdminDashboard.cshtml");
        }

        // GET: /Admin/ManageUsers
        // Fulfills "manageUsers()" by listing Admins. Can be expanded for Doctors/Patients.
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                TempData["ErrorMessage"] = "Please log in as admin.";
                return RedirectToAction("AdminLogin");
            }

            var adminsFromDb = await _adminService.GetAllAdminsAsync();
            var adminDisplayViewModels = adminsFromDb.Select(a => new AdminDisplayViewModel
            {
                AdminId = a.AdminId,
                Name = a.Name,
                Email = a.Email,
                Role = a.Role
            }).ToList();

            var viewModel = new AdminManageUsersViewModel
            {
                Admins = adminDisplayViewModels
            };

            if (TempData.ContainsKey("AdminManagementMessage")) ViewBag.SuccessMessage = TempData["AdminManagementMessage"];
            if (TempData.ContainsKey("AdminManagementError")) ViewBag.ErrorMessage = TempData["AdminManagementError"];

            // URGENT: Standard path would be ~/Views/Admin/ManageUsers.cshtml
            // You mentioned your view is AdminManagement.cshtml, so using that path.
            return View("~/Views/Home/AdminManagement.cshtml", viewModel);
        }

        // GET: /Admin/AddAdmin
        // Fulfills "addAdmin()"
        [HttpGet]
        // [Authorize(Roles = "SuperAdmin")] // Example for role-based authorization
        public IActionResult AddAdmin()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                TempData["ErrorMessage"] = "Please log in to perform this action.";
                return RedirectToAction("AdminLogin");
            }
            // Optional: Add role-based check here from session if needed
            // var currentAdminRole = HttpContext.Session.GetString("AdminRole");
            // if (currentAdminRole != "SuperAdmin") { ... return RedirectToAction("ManageUsers", new { ErrorMessage = "Unauthorized"}); }

            // URGENT: Standard path would be ~/Views/Admin/AddAdmin.cshtml
            return View("~/Views/Admin/AddAdmin.cshtml", new AdminAddViewModel());
        }

        // POST: /Admin/AddAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
        // [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> AddAdmin(AdminAddViewModel model)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                TempData["ErrorMessage"] = "Your session has expired or you are not logged in. Please log in again.";
                return RedirectToAction("AdminLogin");
            }

            if (ModelState.IsValid)
            {
                var existingAdmin = await _adminService.GetAdminByEmailAsync(model.Email);
                if (existingAdmin != null)
                {
                    ModelState.AddModelError("Email", "An admin with this email address already exists.");
                    // URGENT: Standard path would be ~/Views/Admin/AddAdmin.cshtml
                    return View("~/Views/Admin/AddAdmin.cshtml", model);
                }

                var admin = new Admin
                {
                    Name = model.Name,
                    Email = model.Email,
                    Role = string.IsNullOrEmpty(model.Role) ? "Admin" : model.Role.Trim()
                };

                bool result = await _adminService.AddAdminAsync(admin, model.Password);
                if (result)
                {
                    TempData["AdminManagementMessage"] = $"Admin '{admin.Name}' added successfully.";
                    // _logger?.LogInformation($"Admin '{admin.Name}' (ID: {admin.AdminId}) added by Admin ID: {HttpContext.Session.GetInt32("AdminId")?.ToString() ?? "N/A"}");
                    return RedirectToAction("ManageUsers");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while adding the admin. Please try again or contact support.");
                }
            }
            // URGENT: Standard path would be ~/Views/Admin/AddAdmin.cshtml
            return View("~/Views/Admin/AddAdmin.cshtml", model);
        }


        // GET: /Admin/ViewSystemLogs
        // Fulfills "viewSystemLogs()"
        [HttpGet]
        public IActionResult ViewSystemLogs()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                TempData["ErrorMessage"] = "Please log in as admin.";
                return RedirectToAction("AdminLogin");
            }

            // This is a placeholder. Real logging would involve a logging service (e.g., Serilog, NLog writing to a file/DB)
            // and fetching logs from there.
            var logs = new List<string> {
                $"{DateTime.Now}: System log viewing initiated by {HttpContext.Session.GetString("AdminName") ?? "Admin"}.",
                $"{DateTime.Now.AddMinutes(-5)}: Placeholder log: User login event.",
                $"{DateTime.Now.AddMinutes(-10)}: Placeholder log: Scheduled task executed."
                // In a real system, these would be actual log entries.
            };
            ViewBag.LogsTitle = "System Event Logs";

            // URGENT: Standard path would be ~/Views/Admin/ViewSystemLogs.cshtml
            return View("~/Views/Admin/ViewSystemLogs.cshtml", logs);
        }

        // POST: /Admin/AdminLogout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogout()
        {
            // _logger?.LogInformation($"Admin logout for AdminId: {HttpContext.Session.GetInt32("AdminId")?.ToString() ?? "N/A"}");
            HttpContext.Session.Clear();
            TempData["GlobalSuccessMessage"] = "You have been successfully logged out as Admin.";
            return RedirectToAction("Index", "Home"); // Or "AdminLogin"
        }

        // GET: /Admin/AdminForgotPassword
        [HttpGet]
        public IActionResult AdminForgotPassword()
        {
            // URGENT: Standard path would be ~/Views/Admin/AdminForgotPassword.cshtml
            return View("~/Views/Home/AdminForgotPassword.cshtml", new AdminForgotPasswordViewModel());
        }

        // POST: /Admin/AdminForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminForgotPassword(AdminForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var admin = await _adminService.GetAdminByEmailAsync(model.Email);
                if (admin != null)
                {
                    // TODO: Implement actual password reset token generation, storage, and email sending logic.
                    // _logger?.LogInformation($"Password reset requested for admin email: {model.Email}");
                    Debug.WriteLine($"Password reset token should be generated for {admin.Email}");
                }
                // Always show a generic message for security (to prevent email enumeration)
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";
                return RedirectToAction("AdminLogin");
            }
            // URGENT: Standard path would be ~/Views/Admin/AdminForgotPassword.cshtml
            return View("~/Views/Home/AdminForgotPassword.cshtml", model);
        }

        // TODO: Implement EditAdmin (GET and POST) and DeleteAdmin (POST) actions
        // for the `manageUsers()` functionality if required.
    }
}