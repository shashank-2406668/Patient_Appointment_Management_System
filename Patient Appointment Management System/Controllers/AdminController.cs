using Microsoft.AspNetCore.Mvc;
using Patient_Appointment_Management_System.ViewModels;
using Patient_Appointment_Management_System.Services;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Patient_Appointment_Management_System.Controllers
{
    // This controller handles all admin actions
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly IPatientService _patientService;
        private readonly IDoctorService _doctorService;
        private readonly ISystemLogService _systemLogService;
        private readonly IConflictService _conflictService;

        // Constructor to get all needed services
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

        // Helper: check if admin is logged in
        private bool IsAdminLoggedIn()
        {
            return HttpContext.Session.GetString("AdminLoggedIn") == "true";
        }

        // ========== ADMIN LOGIN ==========
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
            if (!ModelState.IsValid) return View("~/Views/Account/AdminLogin.cshtml", model);

            var admin = await _adminService.GetAdminByEmailAsync(model.Email);
            if (admin != null && PasswordHelper.VerifyPassword(model.Password, admin.PasswordHash))
            {
                // Set session for admin
                HttpContext.Session.SetString("AdminLoggedIn", "true");
                HttpContext.Session.SetInt32("AdminId", admin.AdminId);
                HttpContext.Session.SetString("AdminName", admin.Name ?? "Admin");
                HttpContext.Session.SetString("AdminRole", admin.Role ?? "Admin");
                TempData["AdminSuccessMessage"] = "Admin login successful!";
                return RedirectToAction("AdminDashboard");
            }
            ModelState.AddModelError("", "Invalid email or password.");
            return View("~/Views/Account/AdminLogin.cshtml", model);
        }

        // ========== ADMIN DASHBOARD ==========
        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");

            ViewBag.SuccessMessage = TempData["AdminSuccessMessage"];
            var dashboard = new AdminDashboardViewModel
            {
                TotalPatients = await _patientService.GetTotalPatientsCountAsync(),
                TotalDoctors = await _doctorService.GetTotalDoctorsCountAsync(),
                RecentSystemLogs = await _systemLogService.GetRecentLogsAsync(10),
                ActiveConflicts = await _conflictService.GetActiveConflictsAsync(5),
                RecentPatients = (await _patientService.GetRecentPatientsAsync(5)).Select(p => new PatientRowViewModel
                {
                    PatientId = p.PatientId,
                    Name = p.Name,
                    Email = p.Email,
                    Phone = p.Phone
                }).ToList(),
                RecentDoctors = (await _doctorService.GetRecentDoctorsAsync(5)).Select(d => new DoctorRowViewModel
                {
                    DoctorId = d.DoctorId,
                    Name = d.Name,
                    Email = d.Email,
                    Phone = d.Phone,
                    Specialization = d.Specialization
                }).ToList()
            };
            return View("~/Views/Admin/AdminDashboard.cshtml", dashboard);
        }

        // ========== MANAGE USERS ==========
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");

            var viewModel = new AdminManageUsersViewModel
            {
                Admins = (await _adminService.GetAllAdminsAsync()).Select(a => new AdminDisplayViewModel
                {
                    AdminId = a.AdminId,
                    Name = a.Name,
                    Email = a.Email,
                    Role = a.Role
                }).ToList(),
                Doctors = (await _doctorService.GetAllDoctorsAsync()).Select(d => new DoctorRowViewModel
                {
                    DoctorId = d.DoctorId,
                    Name = d.Name,
                    Email = d.Email,
                    Phone = d.Phone,
                    Specialization = d.Specialization
                }).ToList(),
                Patients = (await _patientService.GetAllPatientsAsync()).Select(p => new PatientRowViewModel
                {
                    PatientId = p.PatientId,
                    Name = p.Name,
                    Email = p.Email,
                    Phone = p.Phone
                }).ToList()
            };
            ViewBag.SuccessMessage = TempData["UserManagementMessage"];
            ViewBag.ErrorMessage = TempData["UserManagementError"];
            return View("~/Views/Admin/ManageUsers.cshtml", viewModel);
        }

        // ========== ADD ADMIN ==========
        [HttpGet]
        public IActionResult AddAdmin()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            return View("~/Views/Admin/AddAdmin.cshtml", new AdminAddViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdmin(AdminAddViewModel model)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            if (!ModelState.IsValid) return View("~/Views/Admin/AddAdmin.cshtml", model);

            var existingAdmin = await _adminService.GetAdminByEmailAsync(model.Email);
            if (existingAdmin != null)
            {
                ModelState.AddModelError("Email", "An admin with this email already exists.");
                return View("~/Views/Admin/AddAdmin.cshtml", model);
            }
            var admin = new Admin { Name = model.Name, Email = model.Email, Role = string.IsNullOrEmpty(model.Role) ? "Admin" : model.Role.Trim() };
            bool result = await _adminService.AddAdminAsync(admin, model.Password);
            if (result)
            {
                TempData["UserManagementMessage"] = $"Admin '{admin.Name}' added successfully.";
                return RedirectToAction("ManageUsers");
            }
            ModelState.AddModelError("", "An error occurred while adding the admin.");
            return View("~/Views/Admin/AddAdmin.cshtml", model);
        }

        // ========== ADD DOCTOR ==========
        [HttpGet]
        public IActionResult AddDoctor()
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            return View("~/Views/Admin/AddDoctor.cshtml", new DoctorRegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctor(DoctorRegisterViewModel model)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            if (!ModelState.IsValid) return View("~/Views/Admin/AddDoctor.cshtml", model);

            var existingDoctor = await _doctorService.GetDoctorByEmailAsync(model.Email);
            if (existingDoctor != null)
            {
                ModelState.AddModelError("Email", "A doctor with this email already exists.");
                return View("~/Views/Admin/AddDoctor.cshtml", model);
            }
            var doctor = new Doctor
            {
                Name = model.Name,
                Email = model.Email,
                Specialization = model.Specialization,
                Phone = $"{model.CountryCode}{model.PhoneNumber}"
            };
            bool result = await _doctorService.AddDoctorAsync(doctor, model.Password);
            if (result)
            {
                TempData["UserManagementMessage"] = $"Doctor '{doctor.Name}' added successfully.";
                return RedirectToAction("ManageUsers");
            }
            ModelState.AddModelError("", "An error occurred while adding the doctor.");
            return View("~/Views/Admin/AddDoctor.cshtml", model);
        }

        // ========== DELETE USERS ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePatient(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            var success = await _patientService.DeletePatientAsync(id);
            TempData["UserManagementMessage"] = success ? $"Patient with ID {id} deleted." : "Could not delete the patient.";
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            var success = await _doctorService.DeleteDoctorAsync(id);
            TempData["UserManagementMessage"] = success ? $"Doctor with ID {id} deleted." : "Could not delete the doctor.";
            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAdmin(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            var currentAdminId = HttpContext.Session.GetInt32("AdminId");
            if (id == currentAdminId)
            {
                TempData["UserManagementError"] = "You cannot delete your own account.";
                return RedirectToAction("ManageUsers");
            }
            var success = await _adminService.DeleteAdminAsync(id);
            TempData["UserManagementMessage"] = success ? $"Admin with ID {id} deleted." : "Could not delete the admin.";
            return RedirectToAction("ManageUsers");
        }

        // ========== EDIT USERS ==========
        [HttpGet]
        public async Task<IActionResult> EditPatient(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            var patient = await _patientService.GetPatientByIdAsync(id);
            if (patient == null) return NotFound();
            return View(patient);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPatient(int id, Patient patient)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            if (id != patient.PatientId) return NotFound();
            if (ModelState.IsValid)
            {
                var original = await _patientService.GetPatientByIdAsync(id);
                if (original == null) return NotFound();
                original.Name = patient.Name;
                original.Email = patient.Email;
                original.Phone = patient.Phone;
                original.Address = patient.Address;
                original.Dob = patient.Dob;
                await _patientService.UpdatePatientAsync(original);
                TempData["UserManagementMessage"] = "Patient details updated.";
                return RedirectToAction("ManageUsers");
            }
            return View(patient);
        }

        [HttpGet]
        public async Task<IActionResult> EditDoctor(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            var doctor = await _doctorService.GetDoctorByIdAsync(id);
            if (doctor == null) return NotFound();
            return View(doctor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(int id, Doctor doctor)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            if (id != doctor.DoctorId) return NotFound();
            if (ModelState.IsValid)
            {
                var original = await _doctorService.GetDoctorByIdAsync(id);
                if (original == null) return NotFound();
                original.Name = doctor.Name;
                original.Email = doctor.Email;
                original.Specialization = doctor.Specialization;
                original.Phone = doctor.Phone;
                await _doctorService.UpdateDoctorAsync(original);
                TempData["UserManagementMessage"] = "Doctor details updated.";
                return RedirectToAction("ManageUsers");
            }
            return View(doctor);
        }

        [HttpGet]
        public async Task<IActionResult> EditAdmin(int id)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            var admin = await _adminService.GetAdminByIdAsync(id);
            if (admin == null) return NotFound();
            return View(admin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdmin(int id, Admin admin)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            if (id != admin.AdminId) return NotFound();
            if (ModelState.IsValid)
            {
                var original = await _adminService.GetAdminByIdAsync(id);
                if (original == null) return NotFound();
                original.Name = admin.Name;
                original.Email = admin.Email;
                original.Role = admin.Role;
                await _adminService.UpdateAdminAsync(original);
                TempData["UserManagementMessage"] = "Admin details updated.";
                return RedirectToAction("ManageUsers");
            }
            return View(admin);
        }

        // ========== SYSTEM LOGS ==========
        [HttpGet]
        public async Task<IActionResult> ViewSystemLogs(int page = 1, string filterLevel = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            if (!IsAdminLoggedIn()) return RedirectToAction("AdminLogin");
            int pageSize = 20;
            var (logs, totalLogs) = await _systemLogService.GetPaginatedLogsAsync(page, pageSize, filterLevel, startDate, endDate);
            var viewModel = new ViewSystemLogsViewModel
            {
                SystemLogs = logs,
                CurrentPage = page,
                TotalPages = (int)System.Math.Ceiling(totalLogs / (double)pageSize),
                TotalLogs = totalLogs,
                FilterLevel = filterLevel,
                FilterStartDate = startDate,
                FilterEndDate = endDate
            };
            return View("~/Views/Admin/ViewSystemLogs.cshtml", viewModel);
        }

        // ========== LOGOUT ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AdminLogout()
        {
            HttpContext.Session.Clear();
            TempData["GlobalSuccessMessage"] = "You have been successfully logged out.";
            return RedirectToAction("Index", "Home");
        }

        // ========== FORGOT PASSWORD ==========
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
                TempData["ForgotPasswordMessage"] = "If an account with that email exists, a reset link has been sent.";
                return RedirectToAction("AdminLogin");
            }
            return View("~/Views/Account/AdminForgotPassword.cshtml", model);
        }

        // ========== CANCEL APPOINTMENT FOR CONFLICT ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointmentForConflict(int appointmentId)
        {
            if (!IsAdminLoggedIn()) return Unauthorized(new { success = false, message = "Admin not logged in." });
            if (appointmentId <= 0) return BadRequest(new { success = false, message = "Invalid Appointment ID." });

            var success = await _conflictService.ResolveConflictByCancellingAppointmentAsync(appointmentId);
            if (success)
                return Json(new { success = true, message = $"Appointment {appointmentId} cancelled." });
            else
                return Json(new { success = false, message = $"Failed to cancel appointment {appointmentId}." });
        }
    }
}
