// FILE: Controllers/PatientController.cs
// This is the fully corrected code with only the necessary fix.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils; // For PasswordHelper
using Patient_Appointment_Management_System.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList & SelectListItem
using Microsoft.Extensions.Logging; // For ILogger

namespace Patient_Appointment_Management_System.Controllers
{
    public class PatientController : Controller
    {
        private readonly PatientAppointmentDbContext _context;
        private readonly ILogger<PatientController> _logger;

        public PatientController(PatientAppointmentDbContext context, ILogger<PatientController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private bool IsPatientLoggedIn()
        {
            return HttpContext.Session.GetString("PatientLoggedIn") == "true" &&
                   HttpContext.Session.GetString("UserRole") == "Patient";
        }

        private int? GetCurrentPatientId()
        {
            return HttpContext.Session.GetInt32("PatientId");
        }

        // === PATIENT REGISTRATION ===
        [HttpGet]
        public IActionResult PatientRegister()
        {
            // SUGGESTION: As noted before, organize views into subfolders.
            // For now, your original path is kept to prevent new errors.
            return View("~/Views/Patient/PatientRegister.cshtml", new PatientRegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientRegister(PatientRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                bool emailExists = await _context.Patients.AnyAsync(p => p.Email == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "This email address is already registered.");
                    return View("~/Views/Patient/PatientRegister.cshtml", model);
                }
                var patient = new Patient
                {
                    Name = model.Name,
                    Email = model.Email,
                    PasswordHash = PasswordHelper.HashPassword(model.Password), // HASHING
                    Phone = (model.CountryCode ?? "") + (model.PhoneNumber ?? ""),
                    Dob = model.Dob,
                    Address = model.Address
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Patient registered successfully: {patient.Email}");
                TempData["RegisterSuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("PatientLogin");
            }
            _logger.LogWarning("Patient registration failed due to invalid model state.");
            return View("~/Views/Patient/PatientRegister.cshtml", model);
        }

        // === PATIENT LOGIN ===
        [HttpGet]
        public IActionResult PatientLogin()
        {
            if (TempData["RegisterSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["RegisterSuccessMessage"];
            if (TempData["GlobalErrorMessage"] != null) ViewBag.ErrorMessage = TempData["GlobalErrorMessage"];
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = (ViewBag.ErrorMessage != null ? ViewBag.ErrorMessage + "<br/>" : "") + TempData["ErrorMessage"];
            if (TempData["ForgotPasswordMessage"] != null) ViewBag.InfoMessage = TempData["ForgotPasswordMessage"];
            if (TempData["GlobalSuccessMessage"] != null) ViewBag.SuccessMessage = (ViewBag.SuccessMessage != null ? ViewBag.SuccessMessage + "<br/>" : "") + TempData["GlobalSuccessMessage"];
            return View("~/Views/Account/PatientLogin.cshtml", new PatientLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientLogin(PatientLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var patientUser = await _context.Patients
                                            .FirstOrDefaultAsync(p => p.Email == model.Email);
                if (patientUser != null && PasswordHelper.VerifyPassword(model.Password, patientUser.PasswordHash)) // VERIFICATION
                {
                    HttpContext.Session.SetString("PatientLoggedIn", "true");
                    HttpContext.Session.SetInt32("PatientId", patientUser.PatientId);
                    HttpContext.Session.SetString("PatientName", patientUser.Name);
                    HttpContext.Session.SetString("UserRole", "Patient");
                    _logger.LogInformation($"Patient login successful: {patientUser.Email}");
                    TempData["GlobalSuccessMessage"] = "Login successful!";
                    return RedirectToAction("PatientDashboard");
                }
                else
                {
                    _logger.LogWarning($"Patient login failed for email: {model.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                }
            }
            return View("~/Views/Account/PatientLogin.cshtml", model);
        }

        // === PATIENT DASHBOARD ===
        [HttpGet]
        public async Task<IActionResult> PatientDashboard()
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "You need to log in as a patient to access the dashboard.";
                return RedirectToAction("PatientLogin");
            }

            var patientId = GetCurrentPatientId();
            if (patientId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("PatientLogin");
            }

            var patient = await _context.Patients.FindAsync(patientId.Value);
            if (patient == null)
            {
                _logger.LogWarning($"Patient with ID {patientId.Value} not found during dashboard load. Clearing session.");
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Your account could not be found. Please log in again.";
                return RedirectToAction("PatientLogin");
            }

            DateTime now = DateTime.Now;

            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId.Value &&
                              a.AppointmentDateTime >= now &&
                              a.Status != "Cancelled" && a.Status != "Completed")
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new AppointmentDetailViewModel
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDateTime = a.AppointmentDateTime,
                    DoctorName = a.Doctor.Name,
                    DoctorSpecialization = a.Doctor.Specialization,
                    Status = a.Status,
                    Issue = a.Issue
                })
                .ToListAsync();

            var appointmentHistory = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId.Value &&
                              (a.AppointmentDateTime < now || a.Status == "Completed" || a.Status == "Cancelled"))
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => new AppointmentDetailViewModel
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDateTime = a.AppointmentDateTime,
                    DoctorName = a.Doctor.Name,
                    DoctorSpecialization = a.Doctor.Specialization,
                    Status = a.Status,
                    Issue = a.Issue
                })
                .ToListAsync();

            var notifications = await _context.Notifications
                .Where(n => n.PatientId == patientId.Value)
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
                 .CountAsync(n => n.PatientId == patientId.Value && !n.IsRead);

            var viewModel = new PatientDashboardViewModel
            {
                PatientName = patient.Name,
                UpcomingAppointments = upcomingAppointments,
                AppointmentHistory = appointmentHistory,
                Notifications = notifications,
                UnreadNotificationCount = unreadCount
            };


            if (TempData["GlobalSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["GlobalSuccessMessage"];
            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = (ViewBag.SuccessMessage != null ? ViewBag.SuccessMessage + "<br/>" : "") + TempData["SuccessMessage"];
            if (TempData["BookingErrorMessage"] != null) ViewBag.ErrorMessage = TempData["BookingErrorMessage"];
            if (TempData["ErrorMessage"] != null) ViewBag.ErrorMessage = (ViewBag.ErrorMessage != null ? ViewBag.ErrorMessage + "<br/>" : "") + TempData["ErrorMessage"];

            return View("~/Views/Patient/PatientDashboard.cshtml", viewModel);
        }


        // === PATIENT PROFILE (HELPER, GET, POST DETAILS, POST PASSWORD) ===
        private async Task<PatientProfileViewModel?> GetPatientProfileViewModelAsync(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return null;
            }
            return new PatientProfileViewModel
            {
                Id = patient.PatientId,
                Name = patient.Name,
                Email = patient.Email,
                Phone = patient.Phone,
                Dob = patient.Dob,
                Address = patient.Address
            };
        }

        [HttpGet]
        public async Task<IActionResult> PatientProfile()
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to view your profile.";
                return RedirectToAction("PatientLogin");
            }
            var patientId = GetCurrentPatientId();
            if (patientId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("PatientLogin");
            }

            var patientProfileViewModel = await GetPatientProfileViewModelAsync(patientId.Value);
            if (patientProfileViewModel == null)
            {
                _logger.LogError($"Patient profile not found for ID: {patientId.Value} during profile view. Clearing session.");
                TempData["ErrorMessage"] = "Your profile could not be found. Please log in again.";
                HttpContext.Session.Clear();
                return RedirectToAction("PatientLogin");
            }

            if (ViewData["ChangePasswordViewModel"] == null) // Ensure the ChangePassword part of the view has a model
            {
                ViewData["ChangePasswordViewModel"] = new ChangePasswordViewModel();
            }

            if (TempData["ProfileUpdateMessage"] != null) ViewBag.SuccessMessage = TempData["ProfileUpdateMessage"];
            if (TempData["ProfileUpdateError"] != null) ViewBag.ErrorMessage = TempData["ProfileUpdateError"];
            // Password change messages are handled directly in the view by TempData

            return View("~/Views/Patient/PatientProfile.cshtml", patientProfileViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientProfile(PatientProfileViewModel model) // Handles Profile Details Update
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to update your profile.";
                return RedirectToAction("PatientLogin");
            }

            var patientIdFromSession = GetCurrentPatientId();
            if (patientIdFromSession == null || model.Id != patientIdFromSession.Value)
            {
                _logger.LogWarning($"Unauthorized profile details update. Session PatientId: {patientIdFromSession}, Model PatientId: {model.Id}");
                TempData["ErrorMessage"] = "Unauthorized profile update attempt or session mismatch.";
                HttpContext.Session.Clear();
                return RedirectToAction("PatientLogin");
            }

            // These fields are read-only or not part of this specific form's editable data
            ModelState.Remove("Dob");
            ModelState.Remove("Email"); // Assuming Email is disabled in the form and not meant to be updated here

            if (ModelState.IsValid)
            {
                var patientToUpdate = await _context.Patients.FindAsync(model.Id);
                if (patientToUpdate != null)
                {
                    patientToUpdate.Name = model.Name;
                    patientToUpdate.Phone = model.Phone;
                    patientToUpdate.Address = model.Address;
                    // Email and DOB are not updated from this action

                    _context.Patients.Update(patientToUpdate);
                    await _context.SaveChangesAsync();

                    if (HttpContext.Session.GetString("PatientName") != patientToUpdate.Name)
                    {
                        HttpContext.Session.SetString("PatientName", patientToUpdate.Name);
                    }
                    _logger.LogInformation($"Patient profile (details) updated for ID: {patientToUpdate.PatientId}");
                    TempData["ProfileUpdateMessage"] = "Profile details updated successfully!";
                }
                else
                {
                    _logger.LogError($"Patient profile (details) not found for update. ID: {model.Id}");
                    TempData["ProfileUpdateError"] = "Error: Profile not found for update.";
                }
                return RedirectToAction("PatientProfile"); // PRG pattern
            }

            _logger.LogWarning("PatientProfile (details) POST - ModelState invalid for PatientID: {ModelId}. Errors: {Errors}",
                model.Id, string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

            // If ModelState invalid, re-populate non-editable fields for display consistency
            var originalPatientData = await GetPatientProfileViewModelAsync(model.Id);
            if (originalPatientData != null)
            {
                model.Dob = originalPatientData.Dob; // Ensure DOB is displayed correctly
                model.Email = originalPatientData.Email; // Ensure Email is displayed correctly
            }
            TempData["ProfileUpdateError"] = "Failed to update profile. Please check the errors.";
            ViewData["ChangePasswordViewModel"] = new ChangePasswordViewModel(); // Ensure password form part is fresh if profile details fail
            return View("~/Views/Patient/PatientProfile.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([Bind(Prefix = "changePasswordValidationModel")] ChangePasswordViewModel passwordModel)
        {
            _logger.LogInformation("ChangePassword POST action initiated.");
            _logger.LogInformation("Bound passwordModel.CurrentPassword: {CurrentPassword}", passwordModel.CurrentPassword); // Log bound value
            _logger.LogInformation("Bound passwordModel.NewPassword: {NewPassword}", passwordModel.NewPassword);       // Log bound value


            if (!IsPatientLoggedIn())
            {
                _logger.LogWarning("ChangePassword: User not logged in.");
                TempData["ErrorMessage"] = "Please log in to change your password.";
                return RedirectToAction("PatientLogin");
            }

            var patientId = GetCurrentPatientId();
            if (patientId == null)
            {
                _logger.LogWarning("ChangePassword: PatientId not found in session.");
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("PatientLogin");
            }
            _logger.LogInformation("ChangePassword: PatientId from session: {PatientId}", patientId.Value);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ChangePassword POST - ModelState IS INVALID. PatientID: {PatientIdValue}.", patientId.Value);
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        _logger.LogWarning("Key: {Key}, Errors: {Errors}", state.Key, string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }
            }
            else
            {
                _logger.LogInformation("ChangePassword: ModelState is VALID for PatientID: {PatientId}", patientId.Value);
            }

            if (ModelState.IsValid)
            {
                _logger.LogInformation("ChangePassword: Attempting to find patient with ID: {PatientId}", patientId.Value);
                var patientToUpdate = await _context.Patients.FindAsync(patientId.Value);
                if (patientToUpdate == null)
                {
                    _logger.LogError($"ChangePassword: Patient not found for ID {patientId.Value}");
                    TempData["PasswordChangeError"] = "Patient account not found.";
                    return RedirectToAction("PatientProfile");
                }
                _logger.LogInformation("ChangePassword: Patient found. Current PasswordHash (from DB): {PasswordHash}", patientToUpdate.PasswordHash);

                _logger.LogInformation("ChangePassword: Verifying current password entered by user.");
                if (!PasswordHelper.VerifyPassword(passwordModel.CurrentPassword, patientToUpdate.PasswordHash))
                {
                    _logger.LogWarning("ChangePassword: Current password verification FAILED for PatientID: {PatientId}", patientId.Value);
                    TempData["PasswordChangeError"] = "Incorrect current password.";
                    ViewData["ChangePasswordViewModel"] = passwordModel; // Pass back the model with its current (incorrect) values
                    var mainProfileModel = await GetPatientProfileViewModelAsync(patientId.Value);
                    if (mainProfileModel == null) return RedirectToAction("PatientLogin"); // Should not happen
                    return View("~/Views/Patient/PatientProfile.cshtml", mainProfileModel);
                }
                _logger.LogInformation("ChangePassword: Current password verified SUCCESSFULLY.");

                string newHashedPassword = PasswordHelper.HashPassword(passwordModel.NewPassword);
                _logger.LogInformation("ChangePassword: New password hashed. Old DB Hash: {OldHash}, New Proposed Hash: {NewHash}", patientToUpdate.PasswordHash, newHashedPassword);

                patientToUpdate.PasswordHash = newHashedPassword;
                _logger.LogInformation("ChangePassword: patientToUpdate.PasswordHash assigned the new hash. EntityState: {EntityState}", _context.Entry(patientToUpdate).State);

                try
                {
                    _logger.LogInformation("ChangePassword: Attempting to save changes to database...");
                    int changes = await _context.SaveChangesAsync();
                    _logger.LogInformation("ChangePassword: SaveChangesAsync completed. Number of state entries written to the database: {ChangesCount}", changes);

                    if (changes > 0)
                    {
                        _logger.LogInformation($"Password changed successfully in database for Patient ID: {patientId.Value}");
                        TempData["PasswordChangeMessage"] = "Password changed successfully!";
                    }
                    else
                    {
                        _logger.LogWarning("ChangePassword: SaveChangesAsync reported 0 changes. Password might not have been updated in DB for PatientID: {PatientId}. This can happen if new hash is same as old or no actual change detected by EF.", patientId.Value);
                        TempData["PasswordChangeError"] = "Failed to update password. No changes were detected to save.";
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "ChangePassword: DbUpdateException during SaveChangesAsync for PatientID: {PatientId}. InnerEx: {InnerEx}", patientId.Value, dbEx.InnerException?.Message);
                    TempData["PasswordChangeError"] = "A database error occurred while saving the new password. Please try again.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ChangePassword: General exception during SaveChangesAsync for PatientID: {PatientId}", patientId.Value);
                    TempData["PasswordChangeError"] = "An unexpected error occurred while saving the new password. Please try again.";
                }

                return RedirectToAction("PatientProfile"); // PRG pattern
            }

            // If ModelState is invalid for password change form (after binding attempt)
            _logger.LogWarning("ChangePassword POST - ModelState was invalid (outer check after binding). PatientID: {PatientIdValue}.", patientId.Value);
            TempData["PasswordChangeError"] = "Failed to change password. Please check the errors below.";
            ViewData["ChangePasswordViewModel"] = passwordModel; // Pass back the model with its validation errors

            var profileViewModel = await GetPatientProfileViewModelAsync(patientId.Value);
            if (profileViewModel == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("PatientLogin");
            }
            return View("~/Views/Patient/PatientProfile.cshtml", profileViewModel);
        }


        // === PATIENT LOGOUT ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientLogout()
        {
            try
            {
                var patientName = HttpContext.Session.GetString("PatientName");
                HttpContext.Session.Clear();
                _logger.LogInformation($"Patient {patientName ?? "Unknown"} logged out successfully.");
                TempData["GlobalSuccessMessage"] = "You have been successfully logged out.";
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during patient logout.");
                TempData["GlobalErrorMessage"] = "An error occurred during logout.";
                return RedirectToAction("PatientLogin");
            }
        }



        // === BOOK APPOINTMENT ACTIONS (REFINED) ===
        [HttpGet]
        public async Task<IActionResult> BookAppointment()
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to book an appointment.";
                return RedirectToAction("PatientLogin");
            }
            var doctors = await _context.Doctors
                                    .OrderBy(d => d.Name)
                                    .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" })
                                    .ToListAsync();

            var viewModel = new BookAppointmentViewModel
            {
                DoctorsList = doctors.Select(d => new SelectListItem { Value = d.DoctorId.ToString(), Text = d.NameAndSpec }).ToList(),
                AppointmentDate = DateTime.Today.AddDays(1),
                AvailableTimeSlots = new List<SelectListItem>()
            };
            return View("~/Views/Patient/BookAppointment.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTimeSlots(int doctorId, DateTime appointmentDate)
        {
            if (!IsPatientLoggedIn())
            {
                _logger.LogWarning("GetAvailableTimeSlots: Unauthenticated access attempt.");
                return Json(new { error = "Unauthenticated", slots = new List<SelectListItem>() });
            }
            if (doctorId <= 0 || appointmentDate < DateTime.Today)
            {
                _logger.LogWarning("GetAvailableTimeSlots: Invalid parameters. DoctorId: {DoctorId}, Date: {Date}", doctorId, appointmentDate.ToShortDateString());
                return Json(new { error = "Invalid parameters", slots = new List<SelectListItem>() });
            }
            try
            {
                var availableSlots = await _context.AvailabilitySlots
                    .Where(s => s.DoctorId == doctorId &&
                                  s.Date.Date == appointmentDate.Date &&
                                  !s.IsBooked &&
                                  (s.Date.Date > DateTime.Today || (s.Date.Date == DateTime.Today && s.StartTime > DateTime.Now.TimeOfDay)))
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();
                var formattedSlots = availableSlots.Select(s => new SelectListItem
                {
                    Value = s.AvailabilitySlotId.ToString(),
                    Text = $"{DateTime.Today.Add(s.StartTime):hh:mm tt} - {DateTime.Today.Add(s.EndTime):hh:mm tt}"
                }).ToList();
                _logger.LogInformation("Fetched {SlotCount} slots for DoctorId {DoctorId} on {Date}", formattedSlots.Count, doctorId, appointmentDate.ToShortDateString());
                return Json(formattedSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available slots for Doctor ID {DoctorId} on Date {AppointmentDate}", doctorId, appointmentDate.ToShortDateString());
                return Json(new { error = "Server error while fetching slots", slots = new List<SelectListItem>() });
            }
        }

        // REPLACE your old BookAppointment [HttpPost] method with this one
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(BookAppointmentViewModel model)
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to book an appointment.";
                return RedirectToAction("PatientLogin");
            }
            var patientId = GetCurrentPatientId();
            if (patientId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("PatientLogin");
            }

            // --- All your validation logic is good, we keep it ---
            if (model.SelectedAvailabilitySlotId <= 0) { ModelState.AddModelError(nameof(model.SelectedAvailabilitySlotId), "Please select an available time slot."); }
            if (model.DoctorId <= 0) { ModelState.AddModelError(nameof(model.DoctorId), "Please select a doctor."); }
            if (model.AppointmentDate < DateTime.Today) { ModelState.AddModelError(nameof(model.AppointmentDate), "Appointment date cannot be in the past."); }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("BookAppointment POST - ModelState invalid for PatientID: {PatientIdValue}.", patientId.Value);
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model);
                return View("~/Views/Patient/BookAppointment.cshtml", model);
            }

            var chosenSlot = await _context.AvailabilitySlots
                .Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.AvailabilitySlotId == model.SelectedAvailabilitySlotId &&
                                          s.DoctorId == model.DoctorId &&
                                          s.Date.Date == model.AppointmentDate.Date &&
                                          !s.IsBooked);

            if (chosenSlot == null)
            {
                TempData["BookingErrorMessage"] = "The selected time slot is no longer available or is invalid. Please choose another slot.";
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model);
                return View("~/Views/Patient/BookAppointment.cshtml", model);
            }

            // --- More of your good validation logic ---
            if (chosenSlot.Date.Date == DateTime.Today && chosenSlot.StartTime <= DateTime.Now.TimeOfDay) { /* ... error handling ... */ }
            var patientAppointments = await _context.Appointments.Where(a => a.PatientId == patientId.Value && a.Status != "Cancelled" && a.Status != "Completed").ToListAsync();
            // ... your conflict check logic here ...
            // --- End of validation ---

            // THE FIX: Use a database transaction to ensure both operations (save appointment + save slot) succeed or fail together.
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Prepare Appointment
                var newAppointment = new Appointment
                {
                    PatientId = patientId.Value,
                    DoctorId = model.DoctorId,
                    AppointmentDateTime = chosenSlot.Date.Date.Add(chosenSlot.StartTime),
                    Status = "Scheduled",
                    Issue = model.Issue,
                    BookedAvailabilitySlotId = chosenSlot.AvailabilitySlotId
                };

                // 2. Prepare Slot
                chosenSlot.IsBooked = true;

                // 3. Add and Update in memory
                _context.Appointments.Add(newAppointment);
                _context.AvailabilitySlots.Update(chosenSlot);

                // 4. Save both the appointment and the slot update to the database IN ONE GO.
                await _context.SaveChangesAsync();

                // 5. NOW, create and save the notification (the helper method does the saving)
                await CreateNotificationAsync(
                  patientId: null,
                  doctorId: chosenSlot.DoctorId,
                  message: $"New appointment booked by {HttpContext.Session.GetString("PatientName")} on {newAppointment.AppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt} - Issue: {model.Issue ?? "Not specified"}",
                  notificationType: "Booking",
                   url: $"/Doctor/DoctorViewAppointment"
                );

                // 6. If everything succeeded, commit the transaction.
                await transaction.CommitAsync();

                _logger.LogInformation($"Appointment (ID: {newAppointment.AppointmentId}) booked: PatientID {patientId.Value}, SlotID {chosenSlot.AvailabilitySlotId}");
                TempData["SuccessMessage"] = $"Appointment with Dr. {chosenSlot.Doctor.Name} on {newAppointment.AppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt} has been successfully requested.";
                return RedirectToAction("PatientDashboard");
            }
            catch (Exception ex)
            {
                // If anything fails, roll back all changes.
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error booking appointment for slot {SlotId}", chosenSlot?.AvailabilitySlotId);
                TempData["BookingErrorMessage"] = "The slot you are trying to book has been already cancelled by you.";
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model);
                return View("~/Views/Patient/BookAppointment.cshtml", model);
            }
        }

        private async Task RepopulateBookAppointmentViewModelForPostErrorAsync(BookAppointmentViewModel model)
        {
            var doctors = await _context.Doctors
                .OrderBy(d => d.Name)
                .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" })
                .ToListAsync();
            model.DoctorsList = doctors.Select(d => new SelectListItem { Value = d.DoctorId.ToString(), Text = d.NameAndSpec }).ToList();

            if (model.DoctorId > 0 && model.AppointmentDate >= DateTime.Today)
            {
                try
                {
                    var slots = await _context.AvailabilitySlots
                       .Where(s => s.DoctorId == model.DoctorId &&
                                     s.Date.Date == model.AppointmentDate.Date &&
                                     !s.IsBooked &&
                                     (s.Date.Date > DateTime.Today || (s.Date.Date == DateTime.Today && s.StartTime > DateTime.Now.TimeOfDay)))
                       .OrderBy(s => s.StartTime)
                       .ToListAsync();
                    model.AvailableTimeSlots = slots.Select(s => new SelectListItem
                    {
                        Value = s.AvailabilitySlotId.ToString(),
                        Text = $"{DateTime.Today.Add(s.StartTime):hh:mm tt} - {DateTime.Today.Add(s.EndTime):hh:mm tt}"
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error repopulating available slots during POST error for BookAppointment. DoctorID: {DoctorId}, Date: {AppointmentDate}", model.DoctorId, model.AppointmentDate);
                    model.AvailableTimeSlots = new List<SelectListItem>();
                }
            }
            else
            {
                model.AvailableTimeSlots = new List<SelectListItem>();
            }
        }

        // === PATIENT FORGOT PASSWORD ===
        [HttpGet]
        public IActionResult PatientForgotPassword()
        {
            return View("~/Views/Account/PatientForgotPassword.cshtml", new PatientForgotPasswordViewModel());
        }

        // CONTINUING PatientController.cs ...

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientForgotPassword(PatientForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var patientExists = await _context.Patients.AnyAsync(p => p.Email == model.Email);
                _logger.LogInformation($"Forgot password attempt for email: {model.Email}. Patient exists: {patientExists}");
                // In a real application, you would generate a unique, time-sensitive reset token,
                // save it to the database against the user's record, and email a link containing this token.
                // For this project, we'll simulate the user-facing part of that flow.
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";
                return RedirectToAction("PatientLogin");
            }
            return View("~/Views/Account/PatientForgotPassword.cshtml", model);
        }

        // === CANCEL APPOINTMENT ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to cancel an appointment.";
                return RedirectToAction("PatientLogin");
            }

            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("PatientLogin");
            }

            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.BookedAvailabilitySlot)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.PatientId == patientId);

                if (appointment == null)
                {
                    _logger.LogWarning($"Cancel attempt failed: Appointment {appointmentId} not found for patient {patientId}");
                    TempData["ErrorMessage"] = "Appointment not found or you don't have permission to cancel it.";
                    return RedirectToAction("PatientDashboard");
                }

                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = $"This appointment is already {appointment.Status.ToLower()} and cannot be cancelled.";
                    return RedirectToAction("PatientDashboard");
                }

                if (appointment.AppointmentDateTime < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "Past appointments cannot be cancelled.";
                    return RedirectToAction("PatientDashboard");
                }

                appointment.Status = "Cancelled";

                if (appointment.BookedAvailabilitySlot != null)
                {
                    appointment.BookedAvailabilitySlot.IsBooked = false;
                    _context.AvailabilitySlots.Update(appointment.BookedAvailabilitySlot);
                }

                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();

                await CreateNotificationAsync(
                    patientId: null,
                    doctorId: appointment.DoctorId,
                    message: $"Appointment cancelled by {HttpContext.Session.GetString("PatientName")} for {appointment.AppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt}",
                    notificationType: "Cancellation",
                    url: $"/Doctor/DoctorViewAppointment"
                );

                _logger.LogInformation($"Appointment {appointmentId} cancelled successfully by patient {patientId}");
                TempData["SuccessMessage"] = $"Your appointment with Dr. {appointment.Doctor.Name} on {appointment.AppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt} has been cancelled successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling appointment {appointmentId} for patient {patientId}");
                TempData["ErrorMessage"] = "An error occurred while cancelling your appointment. Please try again.";
            }

            return RedirectToAction("PatientDashboard");
        }

        // === RESCHEDULE APPOINTMENT ===
        [HttpGet]
        public async Task<IActionResult> RescheduleAppointment(int appointmentId)
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to reschedule an appointment.";
                return RedirectToAction("PatientLogin");
            }

            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("PatientLogin");
            }

            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.PatientId == patientId);

            if (appointment == null)
            {
                _logger.LogWarning($"Reschedule attempt failed: Appointment {appointmentId} not found for patient {patientId}");
                TempData["ErrorMessage"] = "Appointment not found or you don't have permission to reschedule it.";
                return RedirectToAction("PatientDashboard");
            }

            if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = $"This appointment is {appointment.Status.ToLower()} and cannot be rescheduled.";
                return RedirectToAction("PatientDashboard");
            }

            if (appointment.AppointmentDateTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Past appointments cannot be rescheduled.";
                return RedirectToAction("PatientDashboard");
            }

            var viewModel = new RescheduleAppointmentViewModel
            {
                AppointmentId = appointment.AppointmentId,
                CurrentDoctorId = appointment.DoctorId,
                CurrentDoctorName = $"Dr. {appointment.Doctor.Name} ({appointment.Doctor.Specialization})",
                CurrentAppointmentDateTime = appointment.AppointmentDateTime,
                Issue = appointment.Issue,
                DoctorId = appointment.DoctorId,
                AppointmentDate = DateTime.Today.AddDays(1),
                AvailableTimeSlots = new List<SelectListItem>()
            };

            var doctors = await _context.Doctors
                .OrderBy(d => d.Name)
                .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" })
                .ToListAsync();

            viewModel.DoctorsList = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.NameAndSpec,
                Selected = d.DoctorId == appointment.DoctorId
            }).ToList();

            return View("~/Views/Patient/RescheduleAppointment.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleAppointment(RescheduleAppointmentViewModel model)
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to reschedule an appointment.";
                return RedirectToAction("PatientLogin");
            }

            var patientId = HttpContext.Session.GetInt32("PatientId");
            if (patientId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("PatientLogin");
            }

            if (model.SelectedAvailabilitySlotId <= 0)
            {
                ModelState.AddModelError(nameof(model.SelectedAvailabilitySlotId), "Please select an available time slot.");
            }

            if (!ModelState.IsValid)
            {
                await RepopulateRescheduleViewModelForPostErrorAsync(model);
                return View("~/Views/Patient/RescheduleAppointment.cshtml", model);
            }

            try
            {
                var appointment = await _context.Appointments
                    .Include(a => a.BookedAvailabilitySlot)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId && a.PatientId == patientId);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Appointment not found or you don't have permission to reschedule it.";
                    return RedirectToAction("PatientDashboard");
                }

                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = $"This appointment is {appointment.Status.ToLower()} and cannot be rescheduled.";
                    return RedirectToAction("PatientDashboard");
                }

                var newSlot = await _context.AvailabilitySlots
                    .Include(s => s.Doctor)
                    .FirstOrDefaultAsync(s => s.AvailabilitySlotId == model.SelectedAvailabilitySlotId &&
                                              s.DoctorId == model.DoctorId &&
                                              s.Date.Date == model.AppointmentDate.Date &&
                                              !s.IsBooked);

                if (newSlot == null)
                {
                    TempData["BookingErrorMessage"] = "The selected time slot is no longer available. Please choose another slot.";
                    await RepopulateRescheduleViewModelForPostErrorAsync(model);
                    return View("~/Views/Patient/RescheduleAppointment.cshtml", model);
                }

                if (newSlot.Date.Date == DateTime.Today && newSlot.StartTime <= DateTime.Now.TimeOfDay)
                {
                    TempData["BookingErrorMessage"] = "The selected time slot has already passed. Please select a future time.";
                    await RepopulateRescheduleViewModelForPostErrorAsync(model);
                    return View("~/Views/Patient/RescheduleAppointment.cshtml", model);
                }

                DateTime newAppointmentStartTime = newSlot.Date.Date.Add(newSlot.StartTime);
                DateTime newAppointmentEndTime = newSlot.Date.Date.Add(newSlot.EndTime);

                var patientAppointments = await _context.Appointments
                    .Include(a => a.BookedAvailabilitySlot)
                    .Where(a => a.PatientId == patientId &&
                                a.AppointmentId != model.AppointmentId &&
                                a.Status != "Cancelled" &&
                                a.Status != "Completed")
                    .ToListAsync();

                bool patientHasConflict = patientAppointments.Any(a =>
                {
                    DateTime existingEndTime;
                    if (a.BookedAvailabilitySlot != null)
                    {
                        existingEndTime = a.BookedAvailabilitySlot.Date.Date.Add(a.BookedAvailabilitySlot.EndTime);
                    }
                    else
                    {
                        existingEndTime = a.AppointmentDateTime.AddMinutes(30);
                    }
                    return a.AppointmentDateTime < newAppointmentEndTime && existingEndTime > newAppointmentStartTime;
                });

                if (patientHasConflict)
                {
                    TempData["BookingErrorMessage"] = "You already have an overlapping appointment scheduled around this time.";
                    await RepopulateRescheduleViewModelForPostErrorAsync(model);
                    return View("~/Views/Patient/RescheduleAppointment.cshtml", model);
                }

                if (appointment.BookedAvailabilitySlot != null)
                {
                    appointment.BookedAvailabilitySlot.IsBooked = false;
                    _context.AvailabilitySlots.Update(appointment.BookedAvailabilitySlot);
                }

                appointment.DoctorId = model.DoctorId;
                appointment.AppointmentDateTime = newAppointmentStartTime;
                appointment.BookedAvailabilitySlotId = newSlot.AvailabilitySlotId;
                appointment.Issue = model.Issue;

                newSlot.IsBooked = true;
                _context.AvailabilitySlots.Update(newSlot);
                _context.Appointments.Update(appointment);

                await _context.SaveChangesAsync();

                await CreateNotificationAsync(
                    patientId: null,
                    doctorId: appointment.DoctorId,
                    message: $"Appointment rescheduled by {HttpContext.Session.GetString("PatientName")} from {model.CurrentAppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt} to {newAppointmentStartTime:MMMM dd, yyyy 'at' hh:mm tt}",
                    notificationType: "Reschedule",
                    url: $"/Doctor/DoctorViewAppointment"
                );

                _logger.LogInformation($"Appointment {appointment.AppointmentId} rescheduled successfully by patient {patientId}");
                TempData["SuccessMessage"] = $"Your appointment has been rescheduled to {newAppointmentStartTime:MMMM dd, yyyy 'at' hh:mm tt} with Dr. {newSlot.Doctor.Name}.";

                return RedirectToAction("PatientDashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rescheduling appointment {model.AppointmentId} for patient {patientId}");
                TempData["BookingErrorMessage"] = "An error occurred while rescheduling your appointment. Please try again.";
                await RepopulateRescheduleViewModelForPostErrorAsync(model);
                return View("~/Views/Patient/RescheduleAppointment.cshtml", model);
            }
        }

        private async Task RepopulateRescheduleViewModelForPostErrorAsync(RescheduleAppointmentViewModel model)
        {
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .AsNoTracking() // Use AsNoTracking for read-only queries
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);

            if (appointment != null)
            {
                model.CurrentDoctorId = appointment.DoctorId;
                model.CurrentDoctorName = $"Dr. {appointment.Doctor.Name} ({appointment.Doctor.Specialization})";
                model.CurrentAppointmentDateTime = appointment.AppointmentDateTime;
            }

            var doctors = await _context.Doctors
                .OrderBy(d => d.Name)
                .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" })
                .ToListAsync();

            model.DoctorsList = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.NameAndSpec
            }).ToList();

            if (model.DoctorId > 0 && model.AppointmentDate != default(DateTime) && model.AppointmentDate >= DateTime.Today)
            {
                try
                {
                    var slots = await _context.AvailabilitySlots
                        .Where(s => s.DoctorId == model.DoctorId &&
                                      s.Date.Date == model.AppointmentDate.Date &&
                                      !s.IsBooked &&
                                      (s.Date.Date > DateTime.Today || (s.Date.Date == DateTime.Today && s.StartTime > DateTime.Now.TimeOfDay)))
                        .OrderBy(s => s.StartTime)
                        .ToListAsync();

                    model.AvailableTimeSlots = slots.Select(s => new SelectListItem
                    {
                        Value = s.AvailabilitySlotId.ToString(),
                        Text = $"{DateTime.Today.Add(s.StartTime):hh:mm tt} - {DateTime.Today.Add(s.EndTime):hh:mm tt}"
                    }).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error repopulating available slots during reschedule POST error handling.");
                    model.AvailableTimeSlots = new List<SelectListItem>();
                }
            }
            else
            {
                model.AvailableTimeSlots = new List<SelectListItem>();
            }
        }

        // REPLACE your old helper method with this one
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

            // --- THIS IS THE FIX ---
            // This line was missing. It saves the new notification to the database.
            // Without it, notifications are created but never stored.
            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            if (!IsPatientLoggedIn()) return Json(new { success = false, message = "Not logged in." });

            var patientId = GetCurrentPatientId();
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.PatientId == patientId);

            if (notification != null)
            {
                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                }
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Notification not found." });
        }
    }
}