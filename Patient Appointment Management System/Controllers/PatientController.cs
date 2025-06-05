// File: Patient_Appointment_Management_System/Controllers/PatientController.cs
// (This is largely the same as the last version you provided, which was already good.
// Key is that BookAppointmentViewModel.SelectedAvailabilitySlotId is an int)
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

        // === PATIENT REGISTRATION ===
        [HttpGet]
        public IActionResult PatientRegister()
        {
            return View("~/Views/Home/PatientRegister.cshtml", new PatientRegisterViewModel());
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
                    return View("~/Views/Home/PatientRegister.cshtml", model);
                }
                var patient = new Patient
                {
                    Name = model.Name,
                    Email = model.Email,
                    PasswordHash = PasswordHelper.HashPassword(model.Password),
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
            return View("~/Views/Home/PatientRegister.cshtml", model);
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
            return View("~/Views/Home/PatientLogin.cshtml", new PatientLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientLogin(PatientLoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var patientUser = await _context.Patients
                                            .FirstOrDefaultAsync(p => p.Email == model.Email);
                if (patientUser != null && PasswordHelper.VerifyPassword(model.Password, patientUser.PasswordHash))
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
            return View("~/Views/Home/PatientLogin.cshtml", model);
        }


        private bool IsPatientLoggedIn()
        {
            return HttpContext.Session.GetString("PatientLoggedIn") == "true" &&
                   HttpContext.Session.GetString("UserRole") == "Patient";
        }

        [HttpGet]
        public async Task<IActionResult> PatientDashboard()
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "You need to log in as a patient to access the dashboard.";
                return RedirectToAction("PatientLogin");
            }

            var patientId = HttpContext.Session.GetInt32("PatientId");
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

            var viewModel = new PatientDashboardViewModel
            {
                PatientName = patient.Name,
                UpcomingAppointments = upcomingAppointments,
                AppointmentHistory = appointmentHistory
            };

            if (TempData["GlobalSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["GlobalSuccessMessage"];
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = (ViewBag.SuccessMessage != null ? ViewBag.SuccessMessage + "<br/>" : "") + TempData["SuccessMessage"];
            }
            if (TempData["BookingErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["BookingErrorMessage"];
            }
            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = (ViewBag.ErrorMessage != null ? ViewBag.ErrorMessage + "<br/>" : "") + TempData["ErrorMessage"];
            }
            return View("~/Views/Home/PatientDashboard.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> PatientProfile()
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to view your profile.";
                return RedirectToAction("PatientLogin");
            }
            var patientIdFromSession = HttpContext.Session.GetInt32("PatientId");
            if (patientIdFromSession == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("PatientLogin");
            }
            var patient = await _context.Patients.FindAsync(patientIdFromSession.Value);
            if (patient == null)
            {
                _logger.LogError($"Patient profile not found for ID: {patientIdFromSession.Value} during profile view. Clearing session.");
                TempData["ErrorMessage"] = "Your profile could not be found. Please log in again.";
                HttpContext.Session.Clear();
                return RedirectToAction("PatientLogin");
            }
            var patientProfileViewModel = new PatientProfileViewModel
            {
                Id = patient.PatientId,
                Name = patient.Name,
                Email = patient.Email,
                Phone = patient.Phone,
                Dob = patient.Dob,
                Address = patient.Address
            };
            if (TempData["ProfileUpdateMessage"] != null) ViewBag.SuccessMessage = TempData["ProfileUpdateMessage"];
            if (TempData["ProfileUpdateError"] != null) ViewBag.ErrorMessage = TempData["ProfileUpdateError"];
            return View("~/Views/Home/PatientProfile.cshtml", patientProfileViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientProfile(PatientProfileViewModel model)
        {
            if (!IsPatientLoggedIn()) return RedirectToAction("PatientLogin");

            var patientIdFromSession = HttpContext.Session.GetInt32("PatientId");
            if (patientIdFromSession == null || model.Id != patientIdFromSession.Value)
            {
                TempData["ErrorMessage"] = "Unauthorized profile update attempt or session mismatch.";
                HttpContext.Session.Clear();
                return RedirectToAction("PatientLogin");
            }

            if (ModelState.IsValid)
            {
                var patientToUpdate = await _context.Patients.FindAsync(model.Id);
                if (patientToUpdate != null)
                {
                    patientToUpdate.Name = model.Name;
                    patientToUpdate.Phone = model.Phone;
                    patientToUpdate.Dob = model.Dob;
                    patientToUpdate.Address = model.Address;

                    _context.Patients.Update(patientToUpdate);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("PatientName", patientToUpdate.Name);
                    _logger.LogInformation($"Patient profile updated for ID: {patientToUpdate.PatientId}");
                    TempData["ProfileUpdateMessage"] = "Profile updated successfully!";
                }
                else
                {
                    _logger.LogError($"Patient profile not found for update. ID: {model.Id}");
                    TempData["ProfileUpdateError"] = "Error: Profile not found for update.";
                }
                return RedirectToAction("PatientProfile");
            }
            return View("~/Views/Home/PatientProfile.cshtml", model);
        }

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
            // Handle TempData for repopulating if redirected from POST with error
            if (TempData.ContainsKey("BookingErrorViewModel"))
            {
                // This part is tricky with AJAX loaded slots.
                // The RepopulateBookAppointmentViewModelForPostErrorAsync should handle setting available slots
                // if doctor and date were valid in the errored submission.
                // However, the simplest is often to let the client-side AJAX reload the slots based on persisted DoctorId/Date.
                // If TempData has error messages, they'll be picked up by ViewBag in the view.
            }
            return View("~/Views/Home/BookAppointment.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTimeSlots(int doctorId, DateTime appointmentDate)
        {
            if (!IsPatientLoggedIn())
            {
                _logger.LogWarning("GetAvailableTimeSlots: Unauthenticated access attempt.");
                return Json(new List<SelectListItem>());
            }

            if (doctorId <= 0 || appointmentDate < DateTime.Today)
            {
                _logger.LogWarning("GetAvailableTimeSlots: Invalid parameters. DoctorId: {DoctorId}, Date: {Date}", doctorId, appointmentDate.ToShortDateString());
                return Json(new List<SelectListItem>());
            }

            try
            {
                var availableSlots = await _context.AvailabilitySlots
                    .Where(s => s.DoctorId == doctorId &&
                                  s.Date.Date == appointmentDate.Date &&
                                  !s.IsBooked &&
                                  (s.Date.Date > DateTime.Today || (s.Date.Date == DateTime.Today && s.StartTime > DateTime.Now.TimeOfDay)))
                    .OrderBy(s => s.StartTime)
                    .ToListAsync(); // First get the data from database

                // Then transform it with proper time formatting
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
                return Json(new List<SelectListItem>());
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(BookAppointmentViewModel model)
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to book an appointment.";
                return RedirectToAction("PatientLogin");
            }

            var patientIdFromSession = HttpContext.Session.GetInt32("PatientId");
            if (patientIdFromSession == null)
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
                _logger.LogWarning("BookAppointment POST - ModelState invalid for PatientID: {PatientId}. Errors: {Errors}",
                    patientIdFromSession.Value,
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            var chosenSlot = await _context.AvailabilitySlots
                                     .Include(s => s.Doctor)
                                     .FirstOrDefaultAsync(s => s.AvailabilitySlotId == model.SelectedAvailabilitySlotId &&
                                                               s.DoctorId == model.DoctorId &&
                                                               s.Date.Date == model.AppointmentDate.Date &&
                                                               !s.IsBooked);

            if (chosenSlot == null)
            {
                _logger.LogWarning("BookAppointment POST - Chosen slot (ID: {SlotId}) for DoctorID {DoctorId} on {Date} not found or already booked for PatientID {PatientId}.",
                    model.SelectedAvailabilitySlotId, model.DoctorId, model.AppointmentDate.ToString("yyyy-MM-dd"), patientIdFromSession.Value);
                TempData["BookingErrorMessage"] = "The selected time slot is no longer available or is invalid. Please choose another slot.";
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            if (chosenSlot.Date.Date == DateTime.Today && chosenSlot.StartTime <= DateTime.Now.TimeOfDay)
            {
                _logger.LogWarning("BookAppointment POST - Chosen slot (ID: {SlotId}) for today has already passed. Current Time: {CurrentTime}, Slot StartTime: {SlotStartTime}",
                    model.SelectedAvailabilitySlotId, DateTime.Now.TimeOfDay, chosenSlot.StartTime);
                TempData["BookingErrorMessage"] = "The selected time slot has already passed for today. Please select a future time.";
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            DateTime newAppointmentStartTime = chosenSlot.Date.Date.Add(chosenSlot.StartTime);
            DateTime newAppointmentEndTime = chosenSlot.Date.Date.Add(chosenSlot.EndTime);

            // FIXED: Fetch appointments first, then check for conflicts in memory
            var patientAppointments = await _context.Appointments
                .Include(a => a.BookedAvailabilitySlot)
                .Where(a => a.PatientId == patientIdFromSession.Value &&
                            a.Status != "Cancelled" &&
                            a.Status != "Completed")
                .ToListAsync();

            // Now check for conflicts in memory
            bool patientHasConflict = patientAppointments.Any(a =>
            {
                DateTime existingEndTime;
                if (a.BookedAvailabilitySlot != null)
                {
                    existingEndTime = a.BookedAvailabilitySlot.Date.Date.Add(a.BookedAvailabilitySlot.EndTime);
                }
                else
                {
                    existingEndTime = a.AppointmentDateTime.AddMinutes(30); // Default 30 minutes
                }

                return a.AppointmentDateTime < newAppointmentEndTime && existingEndTime > newAppointmentStartTime;
            });

            if (patientHasConflict)
            {
                _logger.LogWarning("BookAppointment POST - PatientID {PatientId} has a conflicting appointment for SlotID {SlotId}.",
                    patientIdFromSession.Value, model.SelectedAvailabilitySlotId);
                TempData["BookingErrorMessage"] = "You already have an overlapping appointment scheduled around this time. Please check your dashboard.";
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            try
            {
                var newAppointment = new Appointment
                {
                    PatientId = patientIdFromSession.Value,
                    DoctorId = model.DoctorId,
                    AppointmentDateTime = newAppointmentStartTime,
                    Status = "Scheduled",
                    Issue = model.Issue,
                    BookedAvailabilitySlotId = chosenSlot.AvailabilitySlotId
                };

                chosenSlot.IsBooked = true;

                _context.Appointments.Add(newAppointment);
                _context.AvailabilitySlots.Update(chosenSlot);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment (ID: {newAppointment.AppointmentId}) booked: PatientID {patientIdFromSession.Value}, SlotID {chosenSlot.AvailabilitySlotId}, DateTime {newAppointmentStartTime}");
                TempData["SuccessMessage"] = $"Appointment with Dr. {chosenSlot.Doctor.Name} on {newAppointmentStartTime:MMMM dd, yyyy 'at' hh:mm tt} has been successfully requested.";
                return RedirectToAction("PatientDashboard");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error booking appointment. Slot ID {SlotId} for PatientID {PatientId}.", chosenSlot.AvailabilitySlotId, patientIdFromSession.Value);
                TempData["BookingErrorMessage"] = "This time slot was just booked by someone else. Please select a different slot.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error booking appointment. Patient ID {PatientId}, Slot ID {SlotId}", patientIdFromSession.Value, chosenSlot?.AvailabilitySlotId);
                TempData["BookingErrorMessage"] = "A database error occurred while booking your appointment. Please try again.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error booking appointment. Patient ID {PatientId}, Slot ID {SlotId}", patientIdFromSession.Value, chosenSlot?.AvailabilitySlotId);
                TempData["BookingErrorMessage"] = "An unexpected error occurred while booking your appointment. Please try again.";
            }

            await RepopulateBookAppointmentViewModelForPostErrorAsync(model);
            return View("~/Views/Home/BookAppointment.cshtml", model);
        }

        private DateTime GetAppointmentEndDateTime(Appointment appointment)
        {
            if (appointment.BookedAvailabilitySlotId.HasValue)
            {
                var slot = _context.AvailabilitySlots.AsNoTracking().FirstOrDefault(s => s.AvailabilitySlotId == appointment.BookedAvailabilitySlotId.Value);
                if (slot != null)
                {
                    return slot.Date.Date.Add(slot.EndTime);
                }
            }
            return appointment.AppointmentDateTime.AddMinutes(30);
        }

        private async Task RepopulateBookAppointmentViewModelForPostErrorAsync(BookAppointmentViewModel model)
        {
            var doctors = await _context.Doctors
                                    .OrderBy(d => d.Name)
                                    .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" })
                                    .ToListAsync();
            model.DoctorsList = doctors.Select(d => new SelectListItem { Value = d.DoctorId.ToString(), Text = d.NameAndSpec }).ToList();

            if (model.DoctorId > 0 && model.AppointmentDate != default(DateTime) && model.AppointmentDate >= DateTime.Today)
            {
                try
                {
                    _logger.LogInformation("Repopulating slots for DoctorId {DoctorId} on {Date} after POST error.", model.DoctorId, model.AppointmentDate.ToShortDateString());

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

                    _logger.LogInformation("Repopulated {SlotCount} slots.", model.AvailableTimeSlots.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error repopulating available slots during POST error handling for DoctorId {DoctorId} on {Date}.", model.DoctorId, model.AppointmentDate.ToShortDateString());
                    model.AvailableTimeSlots = new List<SelectListItem>();
                }
            }
            else
            {
                _logger.LogInformation("Skipping slot repopulation due to invalid DoctorId or AppointmentDate in model during POST error.");
                model.AvailableTimeSlots = new List<SelectListItem>();
            }
        }


        // === PATIENT FORGOT PASSWORD ===
        [HttpGet]
        public IActionResult PatientForgotPassword()
        {
            return View("~/Views/Home/PatientForgotPassword.cshtml", new PatientForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientForgotPassword(PatientForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var patientExists = await _context.Patients.AnyAsync(p => p.Email == model.Email);
                _logger.LogInformation($"Forgot password attempt for email: {model.Email}. Patient exists: {patientExists}");
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";
                return RedirectToAction("PatientLogin");
            }
            return View("~/Views/Home/PatientForgotPassword.cshtml", model);
        }
        // Add these methods to your existing PatientController class:

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

                // Check if appointment can be cancelled
                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = $"This appointment is already {appointment.Status.ToLower()} and cannot be cancelled.";
                    return RedirectToAction("PatientDashboard");
                }

                // Check if appointment is in the past
                if (appointment.AppointmentDateTime < DateTime.Now)
                {
                    TempData["ErrorMessage"] = "Past appointments cannot be cancelled.";
                    return RedirectToAction("PatientDashboard");
                }

                // Update appointment status
                appointment.Status = "Cancelled";

                // Free up the availability slot if it exists
                if (appointment.BookedAvailabilitySlot != null)
                {
                    appointment.BookedAvailabilitySlot.IsBooked = false;
                    _context.AvailabilitySlots.Update(appointment.BookedAvailabilitySlot);
                }

                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();

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

            // Check if appointment can be rescheduled
            if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = $"This appointment is {appointment.Status.ToLower()} and cannot be rescheduled.";
                return RedirectToAction("PatientDashboard");
            }

            // Check if appointment is in the past
            if (appointment.AppointmentDateTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Past appointments cannot be rescheduled.";
                return RedirectToAction("PatientDashboard");
            }

            // Create view model for rescheduling
            var viewModel = new RescheduleAppointmentViewModel
            {
                AppointmentId = appointment.AppointmentId,
                CurrentDoctorId = appointment.DoctorId,
                CurrentDoctorName = $"Dr. {appointment.Doctor.Name} ({appointment.Doctor.Specialization})",
                CurrentAppointmentDateTime = appointment.AppointmentDateTime,
                Issue = appointment.Issue,
                DoctorId = appointment.DoctorId, // Pre-select current doctor
                AppointmentDate = DateTime.Today.AddDays(1), // Default to tomorrow
                AvailableTimeSlots = new List<SelectListItem>()
            };

            // Get all doctors for dropdown
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

            return View("~/Views/Home/RescheduleAppointment.cshtml", viewModel);
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
                return View("~/Views/Home/RescheduleAppointment.cshtml", model);
            }

            try
            {
                // Get the original appointment
                var appointment = await _context.Appointments
                    .Include(a => a.BookedAvailabilitySlot)
                    .Include(a => a.Doctor)
                    .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId && a.PatientId == patientId);

                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Appointment not found or you don't have permission to reschedule it.";
                    return RedirectToAction("PatientDashboard");
                }

                // Check if appointment can still be rescheduled
                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    TempData["ErrorMessage"] = $"This appointment is {appointment.Status.ToLower()} and cannot be rescheduled.";
                    return RedirectToAction("PatientDashboard");
                }

                // Get the new slot
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
                    return View("~/Views/Home/RescheduleAppointment.cshtml", model);
                }

                // Check if new slot is in the future
                if (newSlot.Date.Date == DateTime.Today && newSlot.StartTime <= DateTime.Now.TimeOfDay)
                {
                    TempData["BookingErrorMessage"] = "The selected time slot has already passed. Please select a future time.";
                    await RepopulateRescheduleViewModelForPostErrorAsync(model);
                    return View("~/Views/Home/RescheduleAppointment.cshtml", model);
                }

                DateTime newAppointmentStartTime = newSlot.Date.Date.Add(newSlot.StartTime);
                DateTime newAppointmentEndTime = newSlot.Date.Date.Add(newSlot.EndTime);

                // Check for conflicts with other appointments (excluding the current one)
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
                    return View("~/Views/Home/RescheduleAppointment.cshtml", model);
                }

                // Free up the old slot if it exists
                if (appointment.BookedAvailabilitySlot != null)
                {
                    appointment.BookedAvailabilitySlot.IsBooked = false;
                    _context.AvailabilitySlots.Update(appointment.BookedAvailabilitySlot);
                }

                // Update the appointment
                appointment.DoctorId = model.DoctorId;
                appointment.AppointmentDateTime = newAppointmentStartTime;
                appointment.BookedAvailabilitySlotId = newSlot.AvailabilitySlotId;
                appointment.Issue = model.Issue;

                // Mark new slot as booked
                newSlot.IsBooked = true;
                _context.AvailabilitySlots.Update(newSlot);
                _context.Appointments.Update(appointment);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment {appointment.AppointmentId} rescheduled successfully by patient {patientId}");
                TempData["SuccessMessage"] = $"Your appointment has been rescheduled to {newAppointmentStartTime:MMMM dd, yyyy 'at' hh:mm tt} with Dr. {newSlot.Doctor.Name}.";

                return RedirectToAction("PatientDashboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error rescheduling appointment {model.AppointmentId} for patient {patientId}");
                TempData["BookingErrorMessage"] = "An error occurred while rescheduling your appointment. Please try again.";
                await RepopulateRescheduleViewModelForPostErrorAsync(model);
                return View("~/Views/Home/RescheduleAppointment.cshtml", model);
            }
        }

        private async Task RepopulateRescheduleViewModelForPostErrorAsync(RescheduleAppointmentViewModel model)
        {
            // Get the original appointment info
            var appointment = await _context.Appointments
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);

            if (appointment != null)
            {
                model.CurrentDoctorId = appointment.DoctorId;
                model.CurrentDoctorName = $"Dr. {appointment.Doctor.Name} ({appointment.Doctor.Specialization})";
                model.CurrentAppointmentDateTime = appointment.AppointmentDateTime;
            }

            // Repopulate doctors list
            var doctors = await _context.Doctors
                .OrderBy(d => d.Name)
                .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" })
                .ToListAsync();

            model.DoctorsList = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.NameAndSpec
            }).ToList();

            // Repopulate time slots if doctor and date are valid
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





    }
}