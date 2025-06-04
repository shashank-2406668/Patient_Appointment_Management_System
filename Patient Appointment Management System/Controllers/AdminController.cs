using Microsoft.AspNetCore.Mvc;
using Patient_Appointment_Management_System.ViewModels; // For all your ViewModels
using Patient_Appointment_Management_System.Services;   // For ALL services (IAdminService, IPatientService, etc.)
using Patient_Appointment_Management_System.Models;     // For Admin, Patient, Doctor models
using Patient_Appointment_Management_System.Utils;      // For PasswordHelper
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;                           // For async operations
// using Microsoft.Extensions.Logging; // If you choose to use ILogger
// using Microsoft.AspNetCore.Authorization; // Uncomment if/when you implement ASP.NET Core Identity or custom auth schemes

namespace Patient_Appointment_Management_System.Controllers
{
    // [Authorize] // You might want to authorize the entire controller once auth is fully set up
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IPatientService _patientService;     // Added
        private readonly IDoctorService _doctorService;       // Added
        private readonly ISystemLogService _systemLogService; // Added
        private readonly IConflictService _conflictService;   // Added
        // private readonly ILogger<AdminController> _logger; // Optional for structured logging

        public AdminController(
            IAdminService adminService,
            IPatientService patientService,     // Added
            IDoctorService doctorService,       // Added
            ISystemLogService systemLogService, // Added
            IConflictService conflictService   // Added
                                               // ILogger<AdminController> logger // Optional
            )
        {
            _adminService = adminService;
            _patientService = patientService;         // Added
            _doctorService = doctorService;           // Added
            _systemLogService = systemLogService;     // Added
            _conflictService = conflictService;       // Added
            // _logger = logger; // Optional
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

            Debug.WriteLine($"Admin Login Attempt - Email: {model.Email}");
            var admin = await _adminService.GetAdminByEmailAsync(model.Email);

            if (admin != null && PasswordHelper.VerifyPassword(model.Password, admin.PasswordHash))
            {
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                HttpContext.Session.SetInt32("AdminId", admin.AdminId);
                HttpContext.Session.SetString("AdminName", admin.Name);
                HttpContext.Session.SetString("AdminRole", admin.Role ?? "Admin");

                TempData["AdminSuccessMessage"] = "Admin login successful!";
                return RedirectToAction("AdminDashboard");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                // URGENT: Standard path would be ~/Views/Admin/AdminLogin.cshtml
                return View("~/Views/Home/AdminLogin.cshtml", model);
            }
        }

        // GET: /Admin/AdminDashboard
        [HttpGet]
        public async Task<IActionResult> AdminDashboard() // Made async
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

            var dashboardViewModel = new AdminDashboardViewModel
            {
                TotalPatients = await _patientService.GetTotalPatientsCountAsync(),
                TotalDoctors = await _doctorService.GetTotalDoctorsCountAsync(),
                RecentSystemLogs = await _systemLogService.GetRecentLogsAsync(10), // Get 10 recent logs
                ActiveConflicts = await _conflictService.GetActiveConflictsAsync(5) // Get 5 conflicts
            };

            var recentPatientsDb = await _patientService.GetRecentPatientsAsync(5);
            dashboardViewModel.RecentPatients = recentPatientsDb.Select(p => new PatientRowViewModel
            {
                PatientId = p.PatientId,
                Name = p.Name,
                Email = p.Email,
                Phone = p.Phone,
                // LastActivity = p.LastLoginTime?.ToString("g") ?? "N/A" // Placeholder
            }).ToList();

            var recentDoctorsDb = await _doctorService.GetRecentDoctorsAsync(5);
            dashboardViewModel.RecentDoctors = recentDoctorsDb.Select(d => new DoctorRowViewModel
            {
                DoctorId = d.DoctorId,
                Name = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Specialization = d.Specialization,
                // LastActivity = d.LastLoginTime?.ToString("g") ?? "N/A" // Placeholder
            }).ToList();

            // URGENT: Standard path would be ~/Views/Admin/AdminDashboard.cshtml
            return View("~/Views/Home/AdminDashboard.cshtml", dashboardViewModel);
        }


        // API endpoint to cancel an appointment (used by JavaScript from the conflict modal)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointmentForConflict(int appointmentId)
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                return Unauthorized(new { message = "Admin not logged in." }); // Return JSON for AJAX
            }

            if (appointmentId <= 0)
            {
                return BadRequest(new { message = "Invalid Appointment ID." }); // Return JSON for AJAX
            }

            var success = await _conflictService.ResolveConflictByCancellingAppointmentAsync(appointmentId);
            if (success)
            {
                // Use the static AddLog method if your SystemLogService has it for simplicity
                // Or inject ISystemLogService and call an instance method
                SystemLogService.AddLog($"Admin (ID: {HttpContext.Session.GetInt32("AdminId")}) cancelled Appointment ID: {appointmentId} due to conflict.");
                return Json(new { success = true, message = $"Appointment {appointmentId} cancelled successfully." });
            }
            return Json(new { success = false, message = $"Failed to cancel appointment {appointmentId} or appointment not found." });
        }


        // GET: /Admin/ManageUsers
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
            return View("~/Views/Home/AdminManagement.cshtml", viewModel);
        }

        // GET: /Admin/AddAdmin
        [HttpGet]
        public IActionResult AddAdmin()
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                TempData["ErrorMessage"] = "Please log in to perform this action.";
                return RedirectToAction("AdminLogin");
            }
            // URGENT: Standard path would be ~/Views/Admin/AddAdmin.cshtml
            return View("~/Views/Admin/AddAdmin.cshtml", new AdminAddViewModel());
        }

        // POST: /Admin/AddAdmin
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                    SystemLogService.AddLog($"Admin '{admin.Name}' created by Admin ID: {HttpContext.Session.GetInt32("AdminId")?.ToString() ?? "N/A"}");
                    TempData["AdminManagementMessage"] = $"Admin '{admin.Name}' added successfully.";
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
        [HttpGet]
        public async Task<IActionResult> ViewSystemLogs() // Made async to match service
        {
            if (HttpContext.Session.GetString("AdminLoggedIn") != "true")
            {
                TempData["ErrorMessage"] = "Please log in as admin.";
                return RedirectToAction("AdminLogin");
            }

            var logs = await _systemLogService.GetRecentLogsAsync(50); // Get more logs for this dedicated page
            ViewBag.LogsTitle = "System Event Logs";

            // URGENT: Standard path would be ~/Views/Admin/ViewSystemLogs.cshtml
            return View("~/Views/Admin/ViewSystemLogs.cshtml", logs);
        }

        // POST: /Admin/AdminLogout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogout()
        {
            SystemLogService.AddLog($"Admin '{HttpContext.Session.GetString("AdminName")}' (ID: {HttpContext.Session.GetInt32("AdminId")?.ToString() ?? "N/A"}) logged out.");
            HttpContext.Session.Clear();
            TempData["GlobalSuccessMessage"] = "You have been successfully logged out as Admin.";
            return RedirectToAction("Index", "Home");
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
                    SystemLogService.AddLog($"Password reset initiated for admin email: {model.Email}.");
                    Debug.WriteLine($"Password reset token should be generated for {admin.Email}");
                }
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";
                return RedirectToAction("AdminLogin");
            }
            // URGENT: Standard path would be ~/Views/Admin/AdminForgotPassword.cshtml
            return View("~/Views/Home/AdminForgotPassword.cshtml", model);
        }
    }
}