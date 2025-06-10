// File: Patient_Appointment_Management_System/Controllers/DoctorController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using Patient_Appointment_Management_System.ViewModels;
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
        private readonly PatientAppointmentDbContext _context;
        private readonly ILogger<DoctorController> _logger;

        public DoctorController(PatientAppointmentDbContext context, ILogger<DoctorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // === DOCTOR REGISTRATION ===
        [HttpGet]
        public IActionResult DoctorRegister() => View("~/Views/Home/DoctorRegister.cshtml", new DoctorRegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorRegister(DoctorRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool emailExists = await _context.Doctors.AnyAsync(d => d.Email == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "This email address is already registered for a doctor.");
                    return View("~/Views/Home/DoctorRegister.cshtml", model);
                }
                var doctor = new Doctor
                {
                    Name = model.Name,
                    Email = model.Email,
                    PasswordHash = PasswordHelper.HashPassword(model.Password),
                    Specialization = model.Specialization,
                    Phone = model.CountryCode + model.PhoneNumber // Assuming Phone is required in Doctor model
                };
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Doctor registered successfully: {doctor.Email}");
                TempData["DoctorRegisterSuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("DoctorLogin");
            }
            return View("~/Views/Home/DoctorRegister.cshtml", model);
        }

        // === DOCTOR LOGIN ===
        [HttpGet]
        public IActionResult DoctorLogin()
        {
            if (TempData["DoctorRegisterSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["DoctorRegisterSuccessMessage"];
            if (TempData["DoctorLogoutMessage"] != null) ViewBag.InfoMessage = TempData["DoctorLogoutMessage"];
            if (TempData["GlobalSuccessMessage"] != null) ViewBag.SuccessMessage = (ViewBag.SuccessMessage != null ? ViewBag.SuccessMessage + "<br/>" : "") + TempData["GlobalSuccessMessage"];
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ErrorMessage"];
            if (TempData["ForgotPasswordMessage"] != null) ViewBag.InfoMessage = (ViewBag.InfoMessage != null ? ViewBag.InfoMessage + "<br/>" : "") + TempData["ForgotPasswordMessage"];
            return View("~/Views/Home/DoctorLogin.cshtml", new DoctorLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorLogin(DoctorLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doctorUser = await _context.Doctors.FirstOrDefaultAsync(d => d.Email == model.Email);
                if (doctorUser != null && PasswordHelper.VerifyPassword(model.Password, doctorUser.PasswordHash))
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
            return View("~/Views/Home/DoctorLogin.cshtml", model);
        }

        private bool IsDoctorLoggedIn() => HttpContext.Session.GetString("DoctorLoggedIn") == "true" && HttpContext.Session.GetString("UserRole") == "Doctor";

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

            var doctor = await _context.Doctors.FindAsync(doctorId.Value);
            if (doctor == null)
            {
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Your account could not be found. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var todaysAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId.Value &&
                              a.AppointmentDateTime.Date == DateTime.Today &&
                              (a.Status == "Scheduled" || a.Status == "Confirmed"))
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new AppointmentSummaryViewModel
                {
                    Id = a.AppointmentId,
                    PatientName = a.Patient.Name, // Ensure Patient.Name exists
                    AppointmentDateTime = a.AppointmentDateTime,
                    Status = a.Status,
                    Issue = a.Issue
                })
                .ToListAsync();

            // In Dashboard method, add this before creating the viewModel:
            var notifications = await _context.Notifications
                .Where(n => n.DoctorId == doctorId.Value)
                .OrderByDescending(n => n.SentDate)
                .Take(10)
                .Select(n => new NotificationViewModel
                {
                    NotificationId = n.NotificationId,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    SentDate = n.SentDate,
                    IsRead = n.IsRead,
                    Url = n.Url
                })
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.DoctorId == doctorId.Value && !n.IsRead);

            // Update viewModel creation:
            var viewModel = new DoctorDashboardViewModel
            {
                DoctorDisplayName = $"Dr. {doctor.Name}",
                TodaysAppointments = todaysAppointments,
                Notifications = notifications,
                UnreadNotificationCount = unreadCount
            };

            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View("~/Views/Home/DoctorDashboard.cshtml", viewModel);
        }

        // === DOCTOR PROFILE ===
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin");

            var doctor = await _context.Doctors.FindAsync(doctorId.Value);
            if (doctor == null) { HttpContext.Session.Clear(); TempData["ErrorMessage"] = "Profile not found."; return RedirectToAction("DoctorLogin"); }

            var vm = new DoctorProfileViewModel { Id = doctor.DoctorId, Name = doctor.Name, Email = doctor.Email, Specialization = doctor.Specialization };
            if (!string.IsNullOrEmpty(doctor.Phone))
            {
                // Your existing phone parsing logic - ensure it handles cases where Phone might not have a country code part.
                if (doctor.Phone.StartsWith("+"))
                {
                    int firstDigitAfterPlus = -1;
                    for (int i = 1; i < doctor.Phone.Length; i++)
                    {
                        if (char.IsDigit(doctor.Phone[i])) { firstDigitAfterPlus = i; break; }
                    }

                    if (firstDigitAfterPlus != -1)
                    {
                        int codeEndIndex = firstDigitAfterPlus;
                        // Attempt to determine a reasonable length for country code (e.g., 1 to 3 digits after '+')
                        // This part might need refinement based on expected phone formats.
                        int potentialCodeLength = 0;
                        while (codeEndIndex < doctor.Phone.Length && char.IsDigit(doctor.Phone[codeEndIndex]) && potentialCodeLength < 3)
                        {
                            codeEndIndex++;
                            potentialCodeLength++;
                        }
                        // If it looks like a country code, split it. Otherwise, might be better to assign full number to PhoneNumber.
                        if (potentialCodeLength > 0 && potentialCodeLength <= 3)
                        {
                            vm.CountryCode = doctor.Phone.Substring(0, codeEndIndex);
                            vm.PhoneNumber = doctor.Phone.Substring(codeEndIndex);
                        }
                        else
                        {
                            vm.PhoneNumber = doctor.Phone; // Or doctor.Phone.Substring(1) to remove '+' if always present
                        }
                    }
                    else { vm.PhoneNumber = doctor.Phone; } // '+' but no following digits? Assign full.
                }
                else { vm.PhoneNumber = doctor.Phone; }
            }

            if (TempData["ProfileSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["ProfileSuccessMessage"];
            if (TempData["ProfileErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ProfileErrorMessage"];
            return View("~/Views/Home/DoctorProfile.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(DoctorProfileViewModel model)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null || model.Id != doctorId.Value) { TempData["ProfileErrorMessage"] = "Session issue or unauthorized access."; return RedirectToAction("Profile"); }

            var doctorToUpdate = await _context.Doctors.FindAsync(model.Id);
            if (doctorToUpdate == null) { TempData["ProfileErrorMessage"] = "Profile not found."; return RedirectToAction("Profile"); }

            if (ModelState.IsValid)
            {
                doctorToUpdate.Name = model.Name;
                doctorToUpdate.Specialization = model.Specialization;
                doctorToUpdate.Phone = (model.CountryCode ?? "") + (model.PhoneNumber ?? ""); // Handle potential nulls

                try
                {
                    _context.Doctors.Update(doctorToUpdate);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("DoctorName", doctorToUpdate.Name); // Update session
                    TempData["ProfileSuccessMessage"] = "Profile updated successfully!";
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating profile for Doctor ID {DoctorId}. Check for unique constraint violations if Email was changed.", model.Id);
                    TempData["ProfileErrorMessage"] = "An error occurred while updating your profile. It might be due to a duplicate email if you changed it.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating profile for Doctor ID {DoctorId}", model.Id);
                    TempData["ProfileErrorMessage"] = "An error occurred while updating your profile.";
                }
                return RedirectToAction("Profile");
            }
            TempData["ProfileErrorMessage"] = "Please correct validation errors.";
            return View("~/Views/Home/DoctorProfile.cshtml", model);
        }
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
            return View("~/Views/Home/DoctorManageAvailability.cshtml", viewModel);
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
            return View("~/Views/Home/DoctorManageAvailability.cshtml", model);
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

                return View("~/Views/Home/DoctorViewAppointment.cshtml", allAppointments);
            }


            // === DOCTOR FORGOT PASSWORD ===
            [HttpGet]
            public IActionResult DoctorForgotPassword() => View("~/Views/Home/DoctorForgotPassword.cshtml", new DoctorForgotPasswordViewModel());

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
            return View("~/Views/Home/DoctorForgotPassword.cshtml", model);
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