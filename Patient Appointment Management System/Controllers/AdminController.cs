using Microsoft.AspNetCore.Mvc;
using Patient_Appointment_Management_System.ViewModels;
using Patient_Appointment_Management_System.Services;
using Patient_Appointment_Management_System.Models; // This brings Patient_Appointment_Management_System.Models.LogLevel into scope
using Patient_Appointment_Management_System.Utils;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Logging; // If this was uncommented, it would also bring Microsoft.Extensions.Logging.LogLevel

namespace Patient_Appointment_Management_System.Controllers
{
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly ISystemLogService _systemLogService;
        private readonly IConflictService _conflictService;

        public AdminController(
            IAdminService adminService,
            IPatientService patientService,
            IDoctorService doctorService,
            ISystemLogService systemLogService,
            IConflictService conflictService)
        {
            _adminService = adminService;
            _patientService = patientService;
            _doctorService = doctorService;
            _systemLogService = systemLogService;
            _conflictService = conflictService;
        }

        private bool IsAdminLoggedIn() => HttpContext.Session.GetString("AdminLoggedIn") == "true";

        private IActionResult RedirectToLoginIfNotAdmin(string actionName = null)
        {
            if (!IsAdminLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in as admin to access this page.";
                if (!string.IsNullOrEmpty(actionName))
                {
                    TempData["ReturnUrl"] = Url.Action(actionName, "Admin");
                }
                return RedirectToAction("AdminLogin");
            }
            return null;
        }

        [HttpGet]
        public IActionResult AdminLogin()
        {
            if (IsAdminLoggedIn()) return RedirectToAction("AdminDashboard");
            return View("~/Views/Account/AdminLogin.cshtml", new AdminLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
        {
            if (IsAdminLoggedIn()) return RedirectToAction("AdminDashboard");

            if (!ModelState.IsValid)
            {
                return View("~/Views/Account/AdminLogin.cshtml", model);
            }

            var admin = await _adminService.GetAdminByEmailAsync(model.Email);
            if (admin != null && PasswordHelper.VerifyPassword(model.Password, admin.PasswordHash))
            {
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                HttpContext.Session.SetInt32("AdminId", admin.AdminId);
                HttpContext.Session.SetString("AdminName", admin.Name ?? "Admin");
                HttpContext.Session.SetString("AdminRole", admin.Role ?? "Admin");

                await _systemLogService.LogEventAsync(new SystemLog
                {
                    EventType = "AdminLoginSuccess",
                    Message = $"Admin '{admin.Name}' (ID: {admin.AdminId}) logged in successfully.",
                    Source = "AdminController",
                    // --- QUALIFIED ENUM ---
                    Level = Patient_Appointment_Management_System.Models.LogLevel.Information.ToString(),
                    UserId = admin.AdminId.ToString()
                });

                TempData["AdminSuccessMessage"] = "Admin login successful!";
                return RedirectToAction("AdminDashboard");
            }
            else
            {
                await _systemLogService.LogEventAsync(new SystemLog
                {
                    EventType = "AdminLoginFailure",
                    Message = $"Failed login attempt for email: {model.Email}.",
                    Source = "AdminController",
                    // --- QUALIFIED ENUM ---
                    Level = Patient_Appointment_Management_System.Models.LogLevel.Warning.ToString()
                });
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View("~/Views/Account/AdminLogin.cshtml", model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            var authResult = RedirectToLoginIfNotAdmin(nameof(AdminDashboard));
            if (authResult != null) return authResult;

            if (TempData.ContainsKey("AdminSuccessMessage"))
            {
                ViewBag.SuccessMessage = TempData["AdminSuccessMessage"];
            }

            var dashboardViewModel = new AdminDashboardViewModel
            {
                TotalPatients = await _patientService.GetTotalPatientsCountAsync(),
                TotalDoctors = await _doctorService.GetTotalDoctorsCountAsync(),
                RecentSystemLogs = await _systemLogService.GetRecentLogsAsync(10),
                ActiveConflicts = await _conflictService.GetActiveConflictsAsync(5)
            };

            var recentPatientsDb = await _patientService.GetRecentPatientsAsync(5);
            dashboardViewModel.RecentPatients = recentPatientsDb.Select(p => new PatientRowViewModel
            {
                PatientId = p.PatientId,
                Name = p.Name,
                Email = p.Email,
                Phone = p.Phone,
            }).ToList();

            var recentDoctorsDb = await _doctorService.GetRecentDoctorsAsync(5);
            dashboardViewModel.RecentDoctors = recentDoctorsDb.Select(d => new DoctorRowViewModel
            {
                DoctorId = d.DoctorId,
                Name = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Specialization = d.Specialization,
            }).ToList();

            return View("~/Views/Admin/AdminDashboard.cshtml", dashboardViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointmentForConflict(int appointmentId)
        {
            var authResult = RedirectToLoginIfNotAdmin();
            // For AJAX, it's better to return an UnauthorizedResult or specific JSON
            if (authResult != null) return Unauthorized(new { success = false, message = "Admin not logged in. Please refresh and log in again." });


            if (appointmentId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid Appointment ID." });
            }

            var success = await _conflictService.ResolveConflictByCancellingAppointmentAsync(appointmentId);
            var adminId = HttpContext.Session.GetInt32("AdminId")?.ToString() ?? "UnknownAdmin";

            if (success)
            {
                await _systemLogService.LogEventAsync(new SystemLog
                {
                    EventType = "ConflictResolved",
                    Message = $"Admin (ID: {adminId}) cancelled Appointment ID: {appointmentId} due to conflict.",
                    Source = "AdminController",
                    // --- QUALIFIED ENUM ---
                    Level = Patient_Appointment_Management_System.Models.LogLevel.Information.ToString(),
                    UserId = adminId
                });
                return Json(new { success = true, message = $"Appointment {appointmentId} cancelled successfully." });
            }
            else
            {
                await _systemLogService.LogEventAsync(new SystemLog
                {
                    EventType = "ConflictResolutionFailure",
                    Message = $"Admin (ID: {adminId}) failed to cancel Appointment ID: {appointmentId}.",
                    Source = "AdminController",
                    // --- QUALIFIED ENUM ---
                    Level = Patient_Appointment_Management_System.Models.LogLevel.Warning.ToString(),
                    UserId = adminId
                });
                return Json(new { success = false, message = $"Failed to cancel appointment {appointmentId} or appointment not found." });
            }
        }

        // Find the ManageUsers() action and replace it with this updated version.
        // In: Controllers/AdminController.cs

        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            var authResult = RedirectToLoginIfNotAdmin(nameof(ManageUsers));
            if (authResult != null) return authResult;

            // Get Admins (Existing Logic)
            var adminsFromDb = await _adminService.GetAllAdminsAsync();
            var adminDisplayViewModels = adminsFromDb.Select(a => new AdminDisplayViewModel
            {
                AdminId = a.AdminId,
                Name = a.Name,
                Email = a.Email,
                Role = a.Role
            }).ToList();

            // Get Doctors (Existing Logic)
            var doctorsFromDb = await _doctorService.GetAllDoctorsAsync();
            var doctorRowViewModels = doctorsFromDb.Select(d => new DoctorRowViewModel
            {
                DoctorId = d.DoctorId,
                Name = d.Name,
                Email = d.Email,
                Phone = d.Phone,
                Specialization = d.Specialization
            }).ToList();

            // ==========================================================
            //          *** ADD THIS NEW LOGIC TO GET PATIENTS ***
            // ==========================================================
            // Assuming you have a GetAllPatientsAsync() method in your IPatientService
            var patientsFromDb = await _patientService.GetAllPatientsAsync();
            var patientRowViewModels = patientsFromDb.Select(p => new PatientRowViewModel
            {
                PatientId = p.PatientId,
                Name = p.Name,
                Email = p.Email,
                Phone = p.Phone
            }).ToList();
            // ==========================================================


            // Populate the single ViewModel with all three lists
            var viewModel = new AdminManageUsersViewModel
            {
                Admins = adminDisplayViewModels,
                Doctors = doctorRowViewModels,
                Patients = patientRowViewModels // Add the new patients list here
            };

            if (TempData["UserManagementMessage"] != null) ViewBag.SuccessMessage = TempData["UserManagementMessage"];
            if (TempData["AdminManagementError"] != null) ViewBag.ErrorMessage = TempData["AdminManagementError"];

            return View("~/Views/Admin/ManageUsers.cshtml", viewModel);
        }

        [HttpGet]
        public IActionResult AddAdmin()
        {
            var authResult = RedirectToLoginIfNotAdmin(nameof(AddAdmin));
            if (authResult != null) return authResult;
            return View("~/Views/Admin/AddAdmin.cshtml", new AdminAddViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdmin(AdminAddViewModel model)
        {
            var authResult = RedirectToLoginIfNotAdmin();
            if (authResult != null) return authResult; // Standard redirect for form posts


            if (ModelState.IsValid)
            {
                var existingAdmin = await _adminService.GetAdminByEmailAsync(model.Email);
                if (existingAdmin != null)
                {
                    ModelState.AddModelError("Email", "An admin with this email address already exists.");
                    return View("~/Views/Admin/AddAdmin.cshtml", model);
                }

                var admin = new Admin
                {
                    Name = model.Name,
                    Email = model.Email,
                    Role = string.IsNullOrEmpty(model.Role) ? "Admin" : model.Role.Trim()
                };

                bool result = await _adminService.AddAdminAsync(admin, model.Password);
                var currentAdminId = HttpContext.Session.GetInt32("AdminId")?.ToString() ?? "System";
                if (result)
                {
                    await _systemLogService.LogEventAsync(new SystemLog
                    {
                        EventType = "AdminCreated",
                        Message = $"Admin '{admin.Name}' (Email: {admin.Email}) created by Admin ID: {currentAdminId}.",
                        Source = "AdminController",
                        // --- QUALIFIED ENUM ---
                        Level = Patient_Appointment_Management_System.Models.LogLevel.Information.ToString(),
                        UserId = currentAdminId
                    });
                    TempData["AdminManagementMessage"] = $"Admin '{admin.Name}' added successfully.";
                    return RedirectToAction("ManageUsers");
                }
                else
                {
                    await _systemLogService.LogEventAsync(new SystemLog
                    {
                        EventType = "AdminCreationFailure",
                        Message = $"Failed to create admin '{model.Name}' (Email: {model.Email}) by Admin ID: {currentAdminId}.",
                        Source = "AdminController",
                        // --- QUALIFIED ENUM ---
                        Level = Patient_Appointment_Management_System.Models.LogLevel.Error.ToString(), // Corrected the specific error line
                        UserId = currentAdminId
                    });
                    ModelState.AddModelError(string.Empty, "An error occurred while adding the admin.");
                }
            }
            return View("~/Views/Admin/AddAdmin.cshtml", model);
        }

        // File: Controllers/AdminController.cs (ViewSystemLogs action)

        [HttpGet]
        public async Task<IActionResult> ViewSystemLogs(int page = 1, string filterLevel = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var authResult = RedirectToLoginIfNotAdmin(nameof(ViewSystemLogs));
            if (authResult != null) return authResult;

            int pageSize = 20; // Or from configuration
            var (logs, totalLogs) = await _systemLogService.GetPaginatedLogsAsync(page, pageSize, filterLevel, startDate, endDate);

            var viewModel = new ViewSystemLogsViewModel // ViewSystemLogsViewModel constructor will populate AvailableLogLevels
            {
                SystemLogs = logs,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalLogs / (double)pageSize),
                TotalLogs = totalLogs,
                FilterLevel = filterLevel,
                FilterStartDate = startDate,
                FilterEndDate = endDate
                // AvailableLogLevels will be set by the ViewModel's constructor
            };

            ViewBag.LogsTitle = "System Event Logs"; // Can be set in the view as well
            return View("~/Views/Admin/ViewSystemLogs.cshtml", viewModel);
        }

        // In: Controllers/AdminController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogout()
        {
            var adminName = HttpContext.Session.GetString("AdminName");
            var adminId = HttpContext.Session.GetInt32("AdminId")?.ToString() ?? "Unknown";

            if (IsAdminLoggedIn())
            {
                await _systemLogService.LogEventAsync(new SystemLog
                {
                    EventType = "AdminLogout",
                    Message = $"Admin '{adminName}' (ID: {adminId}) logged out.",
                    Source = "AdminController",
                    Level = Patient_Appointment_Management_System.Models.LogLevel.Information.ToString(),
                    UserId = adminId
                });
            }

            HttpContext.Session.Clear();
            // This message will now appear on the home page after redirection.
            TempData["GlobalSuccessMessage"] = "You have been successfully logged out.";

            // THE FIX: Redirect to the "Index" action of the "Home" controller.
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AdminForgotPassword()
        {
            if (IsAdminLoggedIn()) return RedirectToAction("AdminDashboard");
            return View("~/Views/Account/AdminForgotPassword.cshtml", new AdminForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminForgotPassword(AdminForgotPasswordViewModel model)
        {
            if (IsAdminLoggedIn()) return RedirectToAction("AdminDashboard");

            if (ModelState.IsValid)
            {
                var admin = await _adminService.GetAdminByEmailAsync(model.Email);
                string eventType = "PasswordResetRequest";
                string message;
                // --- QUALIFY ENUM HERE ---
                Patient_Appointment_Management_System.Models.LogLevel level = Patient_Appointment_Management_System.Models.LogLevel.Information;

                if (admin != null)
                {
                    message = $"Password reset initiated for admin email: {model.Email} (Admin ID: {admin.AdminId}).";
                    Debug.WriteLine($"Password reset token should be generated for {admin.Email}");
                }
                else
                {
                    message = $"Password reset attempt for non-existent admin email: {model.Email}.";
                    // --- QUALIFY ENUM HERE ---
                    level = Patient_Appointment_Management_System.Models.LogLevel.Warning;
                }
                await _systemLogService.LogEventAsync(new SystemLog
                {
                    EventType = eventType,
                    Message = message,
                    Source = "AdminController",
                    Level = level.ToString() // Use the qualified enum variable
                });

                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";
                return RedirectToAction("AdminLogin");
            }
            return View("~/Views/Account/AdminForgotPassword.cshtml", model);
        }



        // Add these methods inside your AdminController class

        [HttpGet]
        public IActionResult AddDoctor()
        {
            var authResult = RedirectToLoginIfNotAdmin(nameof(AddDoctor));
            if (authResult != null) return authResult;
            return View("~/Views/Admin/AddDoctor.cshtml", new DoctorRegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(DoctorRegisterViewModel model)
        {
            var authResult = RedirectToLoginIfNotAdmin();
            if (authResult != null) return authResult;

            if (ModelState.IsValid)
            {
                var existingDoctor = await _doctorService.GetDoctorByEmailAsync(model.Email);
                if (existingDoctor != null)
                {
                    ModelState.AddModelError("Email", "A doctor with this email address already exists.");
                    return View("~/Views/Admin/AddDoctor.cshtml", model);
                }
                var doctor = new Doctor
                {
                    Name = model.Name,
                    Email = model.Email,
                    Specialization = model.Specialization,
                    Phone = $"{model.CountryCode}{model.PhoneNumber}",
                };
                bool result = await _doctorService.AddDoctorAsync(doctor, model.Password);
                var currentAdminId = HttpContext.Session.GetInt32("AdminId")?.ToString() ?? "System";

                if (result)
                {
                    await _systemLogService.LogEventAsync(new SystemLog
                    {
                        EventType = "DoctorCreated",
                        Message = $"Doctor '{doctor.Name}' (Email: {doctor.Email}) created by Admin ID: {currentAdminId}.",
                        Source = "AdminController",
                        Level = Patient_Appointment_Management_System.Models.LogLevel.Information.ToString(),
                        UserId = currentAdminId
                    });
                    // Use a consistent TempData key for all user management actions
                    TempData["UserManagementMessage"] = $"Doctor '{doctor.Name}' added successfully.";
                    return RedirectToAction("ManageUsers");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "An error occurred while adding the doctor.");
                }
            }
            return View("~/Views/Admin/AddDoctor.cshtml", model);
        }

        // --- DELETE ACTIONS ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePatient(int id)
        {
            var success = await _patientService.DeletePatientAsync(id);
            if (success)
            {
                TempData["UserManagementMessage"] = $"Patient with ID {id} has been deleted.";
            }
            else
            {
                TempData["UserManagementError"] = "Error: Could not delete the patient.";
            }
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var success = await _doctorService.DeleteDoctorAsync(id);
            if (success)
            {
                TempData["UserManagementMessage"] = $"Doctor with ID {id} has been deleted.";
            }
            else
            {
                TempData["UserManagementError"] = "Error: Could not delete the doctor.";
            }
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            var currentAdminId = HttpContext.Session.GetInt32("AdminId");
            if (id == currentAdminId)
            {
                TempData["UserManagementError"] = "Error: You cannot delete your own account.";
                return RedirectToAction("ManageUsers");
            }

            // Add more safety checks if needed (e.g., check if it's the last admin)

            var success = await _adminService.DeleteAdminAsync(id);
            if (success)
            {
                TempData["UserManagementMessage"] = $"Admin with ID {id} has been deleted.";
            }
            else
            {
                TempData["UserManagementError"] = "Error: Could not delete the admin.";
            }
            return RedirectToAction("ManageUsers");
        }


        // --- EDIT ACTIONS (PATIENT) ---

        [HttpGet]
        public async Task<IActionResult> EditPatient(int id)
        {
            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null) return NotFound();

            // Here, you would map to a specific EditPatientViewModel if needed.
            // For simplicity, we are passing the model directly.
            return View(patient);
        }
        // In: Controllers/AdminController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPatient(int id, Patient patient)
        {
            if (id != patient.PatientId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original patient from DB to preserve the password hash
                    var originalPatient = await _patientService.GetPatientByIdAsync(id);
                    if (originalPatient == null)
                    {
                        return NotFound();
                    }

                    // Copy over the new values, but keep the old password
                    originalPatient.Name = patient.Name;
                    originalPatient.Email = patient.Email;
                    originalPatient.Phone = patient.Phone;
                    originalPatient.Address = patient.Address; // Add other fields as needed
                    originalPatient.Dob = patient.Dob;

                    // Now update the original patient object
                    await _patientService.UpdatePatientAsync(originalPatient);

                    TempData["UserManagementMessage"] = "Patient details updated successfully.";
                    return RedirectToAction(nameof(ManageUsers));
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Handle the case where the record might not exist anymore
                    if (await _patientService.GetPatientByIdAsync(id) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            // If ModelState is not valid, return to the view with the current data to show errors
            return View(patient);
        }

        // Add these two methods inside your AdminController.cs

        // GET: /Admin/EditDoctor/5
        [HttpGet]
        public async Task<IActionResult> EditDoctor(int id)
        {
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null)
            {
                return NotFound(); // Returns a 404 if no doctor is found with that ID
            }
            // This will look for a view at /Views/Admin/EditDoctor.cshtml
            return View(doctor);
        }

        // POST: /Admin/EditDoctor/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(int id, Doctor doctor)
        {
            if (id != doctor.DoctorId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Get the original doctor from DB to preserve the password hash
                    var originalDoctor = await _doctorService.GetDoctorByIdAsync(id);
                    if (originalDoctor == null)
                    {
                        return NotFound();
                    }

                    // Copy over the new values, but keep the old password
                    originalDoctor.Name = doctor.Name;
                    originalDoctor.Email = doctor.Email;
                    originalDoctor.Specialization = doctor.Specialization;
                    originalDoctor.Phone = doctor.Phone;

                    // Now update the original doctor object
                    await _doctorService.UpdateDoctorAsync(originalDoctor);

                    TempData["UserManagementMessage"] = "Doctor details updated successfully.";
                    return RedirectToAction(nameof(ManageUsers));
                }
                catch (DbUpdateConcurrencyException)
                {
                    // Handle the case where the record might not exist anymore
                    if (await _doctorService.GetDoctorByIdAsync(id) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            // If the model state is not valid, return to the edit page to show errors
            return View(doctor);
        }
        // Add these two methods inside your AdminController.cs file

        // GET: /Admin/EditAdmin/1
        [HttpGet]
        public async Task<IActionResult> EditAdmin(int id)
        {
            var admin = await _adminService.GetAdminByIdAsync(id);
            if (admin == null)
            {
                return NotFound();
            }
            // This will look for the view at /Views/Admin/EditAdmin.cshtml
            return View(admin);
        }

        // POST: /Admin/EditAdmin/1
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdmin(int id, Admin admin)
        {
            if (id != admin.AdminId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalAdmin = await _adminService.GetAdminByIdAsync(id);
                    if (originalAdmin == null)
                    {
                        return NotFound();
                    }

                    // Copy new values, but preserve the password hash
                    originalAdmin.Name = admin.Name;
                    originalAdmin.Email = admin.Email;
                    originalAdmin.Role = admin.Role;

                    await _adminService.UpdateAdminAsync(originalAdmin);

                    TempData["UserManagementMessage"] = "Admin details updated successfully.";
                    return RedirectToAction(nameof(ManageUsers));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (await _adminService.GetAdminByIdAsync(id) == null)
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(admin);
        }
        // NOTE: You will need to create the corresponding EditDoctor and EditAdmin
        // GET and POST actions, and their respective Views, following the same
        // pattern as EditPatient.
    }
}