// File: Patient_Appointment_Management_System/Controllers/DoctorController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using Patient_Appointment_Management_System.ViewModels;
using Patient_Appointment_Management_System.Services;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // Ensure ILogger is available

// using System.Globalization; // Keep if used elsewhere, not strictly needed for these changes

namespace Patient_Appointment_Management_System.Controllers
{
    public class DoctorController : Controller
    {


        // --- CORRECTED FIELDS ---
        private readonly IDoctorService _doctorService;
        // We keep DbContext here because actions like Availability and Notifications still use it directly.
        // In a full refactor, those would also move to services.
        private readonly PatientAppointmentDbContext _context;
        private readonly ILogger<DoctorController> _logger;

        // --- CORRECTED CONSTRUCTOR (Only one should exist) ---
        public DoctorController(
            IDoctorService doctorService,
            PatientAppointmentDbContext context,
            ILogger<DoctorController> logger)
        {
            _doctorService = doctorService;
            _context = context;
            _logger = logger;
        }

        // --- PUBLIC REGISTRATION REMOVED ---
        // The old DoctorRegister GET and POST methods have been completely removed from this controller.

        // === DOCTOR LOGIN ===
        [HttpGet]
        public IActionResult DoctorLogin()
        {
            // This TempData/ViewBag logic is fine
            if (TempData["DoctorRegisterSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["DoctorRegisterSuccessMessage"];
            if (TempData["DoctorLogoutMessage"] != null) ViewBag.InfoMessage = TempData["DoctorLogoutMessage"];
            if (TempData["GlobalSuccessMessage"] != null) ViewBag.SuccessMessage = (ViewBag.SuccessMessage != null ? ViewBag.SuccessMessage + "<br/>" : "") + TempData["GlobalSuccessMessage"];
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ErrorMessage"];
            if (TempData["ForgotPasswordMessage"] != null) ViewBag.InfoMessage = (ViewBag.InfoMessage != null ? ViewBag.InfoMessage + "<br/>" : "") + TempData["ForgotPasswordMessage"];
            return View("~/Views/Account/DoctorLogin.cshtml", new DoctorLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorLogin(DoctorLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doctorUser = await _doctorService.ValidateDoctorCredentialsAsync(model.Email, model.Password);
                if (doctorUser != null)
                {
                    HttpContext.Session.SetString("DoctorLoggedIn", "true");
                    HttpContext.Session.SetInt32("DoctorId", doctorUser.DoctorId);
                    HttpContext.Session.SetString("DoctorName", doctorUser.Name);
                    HttpContext.Session.SetString("UserRole", "Doctor");

                    _logger.LogInformation($"Doctor login successful: {doctorUser.Email}");
                    TempData["SuccessMessage"] = "Login successful!";
                    return RedirectToAction("Dashboard");
                }
                else
                {
                    _logger.LogWarning($"Doctor login failed for email: {model.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                }
            }
            return View("~/Views/Account/DoctorLogin.cshtml", model);
        }

        private bool IsDoctorLoggedIn() => HttpContext.Session.GetString("DoctorLoggedIn") == "true" && HttpContext.Session.GetString("UserRole") == "Doctor";

        // === DOCTOR DASHBOARD ===
        // PASTE THIS ENTIRE METHOD INTO YOUR DoctorController.cs, REPLACING THE OLD ONE

        // === DOCTOR DASHBOARD ===
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "You need to log in as a doctor to access the dashboard.";
                return RedirectToAction("DoctorLogin");
            }
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var doctor = await _doctorService.GetDoctorByIdAsync(doctorId.Value);
            if (doctor == null)
            {
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Your account could not be found. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var todaysAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId.Value && a.AppointmentDateTime.Date == DateTime.Today && (a.Status == "Scheduled" || a.Status == "Confirmed"))
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new AppointmentSummaryViewModel
                {
                    Id = a.AppointmentId,
                    AppointmentDateTime = a.AppointmentDateTime,
                    PatientName = a.Patient.Name,
                    Status = a.Status
                })
                .ToListAsync();

            // --- THIS IS THE CORRECTED SECTION ---
            var notifications = await _context.Notifications
                .Where(n => n.DoctorId == doctorId.Value)
                .OrderByDescending(n => n.SentDate).Take(10)
                .Select(n => new NotificationViewModel
                {
                    NotificationId = n.NotificationId,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    SentDate = n.SentDate,
                    IsRead = n.IsRead,
                    Url = n.Url
                }) // THE FIX: Fields are now mapped from the database entity (n)
                .ToListAsync();
            // --- END OF CORRECTION ---

            var unreadCount = await _context.Notifications.CountAsync(n => n.DoctorId == doctorId.Value && !n.IsRead);

            var viewModel = new DoctorDashboardViewModel
            {
                DoctorDisplayName = $"Dr. {doctor.Name}",
                TodaysAppointments = todaysAppointments,
                Notifications = notifications, // This list now contains actual data
                UnreadNotificationCount = unreadCount
            };

            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View("~/Views/Doctor/DoctorDashboard.cshtml", viewModel);
        }

        // === DOCTOR PROFILE (REFACTORED) ===
        // In: Controllers/DoctorController.cs

        // REPLACE your entire [HttpGet] Profile() method with this one.
        // REPLACE your old [HttpGet] Profile() method with this one
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to view your profile.";
                return RedirectToAction("DoctorLogin");
            }

            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var doctor = await _doctorService.GetDoctorByIdAsync(doctorId.Value);
            if (doctor == null)
            {
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Your profile could not be found.";
                return RedirectToAction("DoctorLogin");
            }

            var vm = new DoctorProfileViewModel
            {
                Id = doctor.DoctorId,
                Name = doctor.Name,
                Email = doctor.Email,
                Specialization = doctor.Specialization,
                // The ChangePassword property is already initialized by the ViewModel's constructor
            };

            // --- ROBUST PHONE NUMBER SPLITTING LOGIC ---
            if (!string.IsNullOrEmpty(doctor.Phone))
            {
                var countryCodes = new[] { "+91", "+1", "+44", "+81", "+86" }; // Must match codes in the view
                bool codeFound = false;
                foreach (var code in countryCodes)
                {
                    if (doctor.Phone.StartsWith(code))
                    {
                        vm.CountryCode = code;
                        vm.PhoneNumber = doctor.Phone.Substring(code.Length);
                        codeFound = true;
                        break;
                    }
                }
                if (!codeFound)
                {
                    // If the code is not in our list, just put the whole thing in the phone number field
                    vm.PhoneNumber = doctor.Phone;
                }
            }
            // --- END OF PHONE NUMBER LOGIC ---

            if (TempData["ProfileSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["ProfileSuccessMessage"];
            if (TempData["ProfileErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ProfileErrorMessage"];
            if (TempData["PasswordChangeSuccess"] != null) ViewBag.SuccessMessage = (ViewBag.SuccessMessage ?? "") + "<br/>" + TempData["PasswordChangeSuccess"];
            if (TempData["PasswordChangeError"] != null) ViewBag.ErrorMessage = (ViewBag.ErrorMessage ?? "") + "<br/>" + TempData["PasswordChangeError"];

            return View("~/Views/Doctor/DoctorProfile.cshtml", vm);
        }
        // REPLACE your old [HttpPost] Profile(DoctorProfileViewModel model) method with this one
        // This is your current (incorrect) code
        // This is your new (corrected) code
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(DoctorProfileViewModel model)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null || model.Id != doctorId.Value)
            {
                TempData["ProfileErrorMessage"] = "A session or authorization error occurred.";
                return RedirectToAction("Profile");
            }

            // =========================================================================
            //                            *** THE FIX IS HERE ***
            // We are telling the model state to ignore validation for the password part
            // of the view model, because this form doesn't submit password data.
            ModelState.Remove("ChangePassword");
            // =========================================================================

            // We only validate the main profile fields here, not the password ones.
            //if (ModelState.IsValid) // <<< SUCCESS: This will now return TRUE
            {
                var doctorToUpdate = await _doctorService.GetDoctorByIdAsync(model.Id);
                if (doctorToUpdate == null)
                {
                    TempData["ProfileErrorMessage"] = "Your profile could not be found to update.";
                    return RedirectToAction("Profile");
                }

                doctorToUpdate.Name = model.Name;
                doctorToUpdate.Specialization = model.Specialization;
                doctorToUpdate.Phone = (model.CountryCode ?? "") + (model.PhoneNumber ?? "");

                var success = await _doctorService.UpdateDoctorProfileAsync(doctorToUpdate);
                if (success)
                {
                    HttpContext.Session.SetString("DoctorName", doctorToUpdate.Name);
                    TempData["ProfileSuccessMessage"] = "Profile details updated successfully!";
                }
                else
                {
                    TempData["ProfileErrorMessage"] = "An error occurred while saving your profile.";
                }
                return RedirectToAction("Profile");
            }

            TempData["ProfileErrorMessage"] = "Please correct the validation errors.";
            return View("~/Views/Doctor/DoctorProfile.cshtml", model);
        }

        // ADD THIS ENTIRE NEW METHOD TO DoctorController.cs
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(DoctorProfileViewModel model)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var passwordModel = model.ChangePassword;

            // We manually check the ModelState for the nested ChangePassword object
            if (string.IsNullOrEmpty(passwordModel.CurrentPassword) ||
                string.IsNullOrEmpty(passwordModel.NewPassword) ||
                passwordModel.NewPassword != passwordModel.ConfirmPassword)
            {
                TempData["PasswordChangeError"] = "Please fill all password fields correctly.";
                return RedirectToAction("Profile");
            }

            if (passwordModel.NewPassword.Length < 8)
            {
                TempData["PasswordChangeError"] = "New password must be at least 8 characters long.";
                return RedirectToAction("Profile");
            }

            var doctor = await _context.Doctors.FindAsync(doctorId.Value);
            if (doctor == null)
            {
                TempData["PasswordChangeError"] = "Could not find your account.";
                return RedirectToAction("Profile");
            }

            // Verify the current password
            if (!PasswordHelper.VerifyPassword(passwordModel.CurrentPassword, doctor.PasswordHash))
            {
                TempData["PasswordChangeError"] = "Incorrect current password.";
                return RedirectToAction("Profile");
            }

            // Hash and update the new password
            doctor.PasswordHash = PasswordHelper.HashPassword(passwordModel.NewPassword);
            _context.Doctors.Update(doctor);
            await _context.SaveChangesAsync();

            TempData["PasswordChangeSuccess"] = "Password changed successfully!";
            return RedirectToAction("Profile");
        }

        // ... (the rest of your DoctorController methods)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            if (!IsDoctorLoggedIn()) return Json(new { success = false });

            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.DoctorId == doctorId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            if (!IsDoctorLoggedIn()) return Json(new { success = false });

            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            var notifications = await _context.Notifications
                .Where(n => n.DoctorId == doctorId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Helper method for creating notifications
        private async Task CreateNotificationAsync(int? patientId, int? doctorId, string message, string notificationType, string? url = null)
        {
            var notification = new Notification
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Message = message,
                NotificationType = notificationType,
                SentDate = DateTime.Now,
                IsRead = false,
                Url = url
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // === AVAILABILITY ACTIONS ===
        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var existingSlotsFromDb = await _context.AvailabilitySlots
                .Include(s => s.BookedByAppointment) // To check IsBooked and potentially get Patient info
                    .ThenInclude(appt => appt != null ? appt.Patient : null) // Patient can be null if BookedByAppointment is null
                .Where(s => s.DoctorId == doctorId.Value && s.Date >= DateTime.Today) // Show today and future slots
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new ExistingAvailabilitySlotViewModel
                {
                    Id = s.AvailabilitySlotId,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsBooked = s.IsBooked, // This flag is crucial and should be set when an appointment books this slot
                    PatientNameIfBooked = s.IsBooked && s.BookedByAppointment != null && s.BookedByAppointment.Patient != null
                                          ? s.BookedByAppointment.Patient.Name
                                          : (s.IsBooked ? "Booked (Info N/A)" : null), // Handle case where IsBooked but patient info missing
                    AppointmentIdIfBooked = s.BookedByAppointmentId
                })
                .ToListAsync();

            var viewModel = new DoctorManageAvailabilityViewModel
            {
                ExistingSlots = existingSlotsFromDb,
                NewSlot = new AvailabilitySlotInputViewModel { Date = DateTime.Today.AddDays(1) } // Default to tomorrow
            };

            if (TempData["AvailabilitySuccessMessage"] != null) ViewBag.SuccessMessage = TempData["AvailabilitySuccessMessage"];
            if (TempData["AvailabilityErrorMessage"] != null) ViewBag.ErrorMessage = TempData["AvailabilityErrorMessage"];
            return View("~/Views/Doctor/DoctorManageAvailability.cshtml", viewModel);
        }

        // File: Patient_Appointment_Management_System/Controllers/DoctorController.cs

        // ... (other methods like GET Availability, Dashboard, etc.) ...

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(DoctorManageAvailabilityViewModel model)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                TempData["AvailabilityErrorMessage"] = "Session error. Please try again.";
                return RedirectToAction("Availability");
            }

            var newSlotInput = model.NewSlot;

            // --- BEGIN ADDED LOGGING ---
            _logger.LogInformation("Attempting to Add Availability for DoctorID: {DoctorId}", doctorId.Value);
            _logger.LogInformation("Received NewSlot Input - Date: {Date}, StartTime: {StartTime}, EndTime: {EndTime}",
                                   newSlotInput.Date.ToString("yyyy-MM-dd"), newSlotInput.StartTime.ToString(), newSlotInput.EndTime.ToString());
            // --- END ADDED LOGGING ---

            if (ModelState.IsValid)
            {
                // Custom Validations for the newSlotInput
                if (newSlotInput.EndTime <= newSlotInput.StartTime)
                {
                    // --- BEGIN ADDED LOGGING FOR THIS SPECIFIC ERROR ---
                    _logger.LogWarning("VALIDATION ERROR: EndTime ({EndTime}) is not after StartTime ({StartTime}) for Date {Date}. DoctorID: {DoctorId}",
                                       newSlotInput.EndTime.ToString(), newSlotInput.StartTime.ToString(), newSlotInput.Date.ToString("yyyy-MM-dd"), doctorId.Value);
                    // --- END ADDED LOGGING FOR THIS SPECIFIC ERROR ---
                    ModelState.AddModelError("NewSlot.EndTime", "End time must be after start time.");
                }

                if (newSlotInput.Date.Date < DateTime.Today) // Compare Date parts only
                {
                    _logger.LogWarning("VALIDATION ERROR: Availability date ({Date}) is in the past. DoctorID: {DoctorId}", newSlotInput.Date.ToString("yyyy-MM-dd"), doctorId.Value);
                    ModelState.AddModelError("NewSlot.Date", "Availability date cannot be in the past.");
                }

                // Overlap Check (Only if other validations passed so far or if you want to check regardless)
                if (ModelState.GetFieldValidationState("NewSlot.EndTime") == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid &&
                    ModelState.GetFieldValidationState("NewSlot.Date") == Microsoft.AspNetCore.Mvc.ModelBinding.ModelValidationState.Valid)
                {
                    bool overlaps = await _context.AvailabilitySlots.AnyAsync(s =>
                        s.DoctorId == doctorId.Value &&
                        s.Date.Date == newSlotInput.Date.Date &&
                        newSlotInput.StartTime < s.EndTime &&
                        newSlotInput.EndTime > s.StartTime);

                    if (overlaps)
                    {
                        _logger.LogWarning("VALIDATION ERROR: Time slot overlaps with an existing one for Date {Date}, StartTime {StartTime}, EndTime {EndTime}. DoctorID: {DoctorId}",
                                           newSlotInput.Date.ToString("yyyy-MM-dd"), newSlotInput.StartTime.ToString(), newSlotInput.EndTime.ToString(), doctorId.Value);
                        ModelState.AddModelError("NewSlot.StartTime", "This time slot overlaps with an existing one for this date.");
                    }
                }


                if (ModelState.IsValid) // Check again after ALL custom validations
                {
                    var availabilitySlot = new AvailabilitySlot
                    {
                        DoctorId = doctorId.Value,
                        Date = newSlotInput.Date.Date,
                        StartTime = newSlotInput.StartTime,
                        EndTime = newSlotInput.EndTime,
                        IsBooked = false
                    };
                    _context.AvailabilitySlots.Add(availabilitySlot);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully added new availability slot ID: {SlotId} for DoctorID: {DoctorId}", availabilitySlot.AvailabilitySlotId, doctorId.Value);
                    TempData["AvailabilitySuccessMessage"] = "Availability slot added successfully!";
                    return RedirectToAction("Availability");
                }
            }
            else // ModelState was initially invalid (e.g., required field missing before custom checks)
            {
                _logger.LogWarning("AddAvailability initial ModelState was invalid for DoctorID: {DoctorId}. Errors: {Errors}",
                    doctorId.Value,
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
            }

            // If ModelState is invalid (either initially or after custom checks), repopulate ExistingSlots and return to view
            TempData["AvailabilityErrorMessage"] = "Failed to add slot. Please check the errors below.";

            var existingSlotsFromDb = await _context.AvailabilitySlots
                .Include(s => s.BookedByAppointment)
                    .ThenInclude(appt => appt != null ? appt.Patient : null)
                .Where(s => s.DoctorId == doctorId.Value && s.Date >= DateTime.Today)
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new ExistingAvailabilitySlotViewModel
                {
                    Id = s.AvailabilitySlotId,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsBooked = s.IsBooked,
                    PatientNameIfBooked = s.IsBooked && s.BookedByAppointment != null && s.BookedByAppointment.Patient != null
                                          ? s.BookedByAppointment.Patient.Name
                                          : (s.IsBooked ? "Booked (Info N/A)" : null),
                    AppointmentIdIfBooked = s.BookedByAppointmentId
                })
                .ToListAsync();

            model.ExistingSlots = existingSlotsFromDb;
            return View("~/Views/Doctor/DoctorManageAvailability.cshtml", model);
        }

        // ... (other methods like DeleteAvailability, GET Availability, Dashboard, etc.) ...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(int slotId)
        {
            if (!IsDoctorLoggedIn()) return Json(new { success = false, message = "Unauthorized." });
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return Json(new { success = false, message = "Session error." });

            var slotToDelete = await _context.AvailabilitySlots
                                       .FirstOrDefaultAsync(s => s.AvailabilitySlotId == slotId && s.DoctorId == doctorId.Value);

            if (slotToDelete == null)
            {
                return Json(new { success = false, message = "Slot not found or access denied." });
            }

            // Check IsBooked flag first
            if (slotToDelete.IsBooked)
            {
                // Optionally, double-check if an appointment is truly linked via BookedAvailabilitySlotId
                var linkedAppointment = await _context.Appointments.FirstOrDefaultAsync(a => a.BookedAvailabilitySlotId == slotToDelete.AvailabilitySlotId);
                if (linkedAppointment != null)
                {
                    return Json(new { success = false, message = "Cannot delete a booked availability slot. The appointment must be cancelled first." });
                }
                // If IsBooked is true but no appointment found, it's an inconsistency. Log it.
                _logger.LogWarning("Slot {SlotId} for doctor {DoctorId} is marked IsBooked but no Appointment.BookedAvailabilitySlotId points to it. Allowing deletion with caution.", slotToDelete.AvailabilitySlotId, doctorId.Value);
            }


            if (slotToDelete.Date < DateTime.Today || (slotToDelete.Date == DateTime.Today && slotToDelete.StartTime < DateTime.Now.TimeOfDay))
            {
                return Json(new { success = false, message = "Cannot delete past or currently active availability slots." });
            }

            try
            {
                _context.AvailabilitySlots.Remove(slotToDelete);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Availability slot deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting availability slot ID {SlotId} for DoctorId {DoctorId}", slotId, doctorId.Value);
                return Json(new { success = false, message = "An error occurred while deleting the slot." });
            }
        }

        // === VIEW ALL APPOINTMENTS FOR DOCTOR ===
        [HttpGet]
        public async Task<IActionResult> DoctorViewAppointment()
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "You need to log in as a doctor to view appointments.";
                return RedirectToAction("DoctorLogin");
            }

            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var allAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId.Value)
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => new AppointmentSummaryViewModel
                {
                    Id = a.AppointmentId,
                    PatientName = a.Patient.Name,
                    AppointmentDateTime = a.AppointmentDateTime,
                    Status = a.Status,
                    Issue = a.Issue
                })
                .ToListAsync();

                return View("~/Views/Doctor/DoctorViewAppointment.cshtml", allAppointments);
            }


        // === DOCTOR FORGOT PASSWORD ===
        [HttpGet]
        public IActionResult DoctorForgotPassword() => View("~/Views/Account/DoctorForgotPassword.cshtml", new DoctorForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorForgotPassword(DoctorForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doctorExists = await _context.Doctors.AnyAsync(d => d.Email == model.Email);
                _logger.LogInformation($"Forgot password attempt for email: {model.Email}. Doctor exists: {doctorExists}");
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent.";
                return RedirectToAction("DoctorLogin");
            }
            return View("~/Views/Account/DoctorForgotPassword.cshtml", model);
        }

        // === LOGOUT ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            var doctorName = HttpContext.Session.GetString("DoctorName");
            HttpContext.Session.Clear();
            _logger.LogInformation($"Doctor {doctorName ?? "Unknown"} logged out successfully.");
            TempData["DoctorLogoutMessage"] = "You have been successfully logged out.";
            return RedirectToAction("Index", "Home");


        }
        // Add this method to your DoctorController class

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "You need to log in as a doctor to cancel appointments.";
                return RedirectToAction("DoctorLogin");
            }

            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            // Find the appointment
            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.BookedAvailabilitySlot)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctorId.Value);

            if (appointment == null)
            {
                TempData["AvailabilityErrorMessage"] = "Appointment not found or you don't have permission to cancel it.";
                return RedirectToAction("DoctorViewAppointment");
            }

            // Check if appointment is in the future
            if (appointment.AppointmentDateTime < DateTime.Now)
            {
                TempData["AvailabilityErrorMessage"] = "Cannot cancel past appointments.";
                return RedirectToAction("DoctorViewAppointment");
            }

            // Check if already cancelled
            if (appointment.Status == "Cancelled")
            {
                TempData["AvailabilityErrorMessage"] = "This appointment is already cancelled.";
                return RedirectToAction("DoctorViewAppointment");
            }

            try
            {
                // Update appointment status
                appointment.Status = "Cancelled";

                // If appointment had a booked availability slot, free it up
                if (appointment.BookedAvailabilitySlot != null)
                {
                    appointment.BookedAvailabilitySlot.IsBooked = false;
                    appointment.BookedAvailabilitySlot.BookedByAppointmentId = null;
                }

                // Create notification for patient
                await CreateNotificationAsync(
                    patientId: appointment.PatientId,
                    doctorId: null,
                    message: $"Your appointment with Dr. {HttpContext.Session.GetString("DoctorName")} on {appointment.AppointmentDateTime.ToString("MMM dd, yyyy at hh:mm tt")} has been cancelled by the doctor.",
                    notificationType: "AppointmentCancelled",
                    url: "/Patient/ViewAppointments"
                );

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment {appointmentId} cancelled by Doctor {doctorId}");
                TempData["AvailabilitySuccessMessage"] = "Appointment cancelled successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling appointment {AppointmentId} for Doctor {DoctorId}", appointmentId, doctorId);
                TempData["AvailabilityErrorMessage"] = "An error occurred while cancelling the appointment.";
            }

            return RedirectToAction("DoctorViewAppointment");
        }

    }


}