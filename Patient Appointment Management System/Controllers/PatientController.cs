// File: Patient_Appointment_Management_System/Controllers/PatientController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using Patient_Appointment_Management_System.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

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
                    Phone = model.CountryCode + model.PhoneNumber,
                    Dob = model.Dob,
                    Address = model.Address
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Patient registered successfully: {patient.Email}");
                TempData["RegisterSuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("PatientLogin");
            }
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
                _logger.LogWarning($"Patient with ID {patientId.Value} not found. Clearing session.");
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
                _logger.LogError($"Patient profile not found for ID: {patientIdFromSession.Value}. Clearing session.");
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
                    TempData["ProfileUpdateMessage"] = "Error: Profile not found for update.";
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

        // === BOOK APPOINTMENT ACTIONS (UPDATED) ===
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
                                    .Select(d => new { d.DoctorId, NameAndSpec = $"{d.Name} ({d.Specialization})" })
                                    .ToListAsync();
            var viewModel = new BookAppointmentViewModel
            {
                DoctorsList = new SelectList(doctors, "DoctorId", "NameAndSpec"),
                AppointmentDate = DateTime.Today.AddDays(1)
            };
            return View("~/Views/Home/BookAppointment.cshtml", viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSlotsForDoctor(int doctorId, DateTime appointmentDate)
        {
            if (!IsPatientLoggedIn())
            {
                return Json(new { success = false, message = "User not logged in." });
            }
            if (doctorId <= 0 || appointmentDate < DateTime.Today)
            {
                return Json(new { success = false, message = "Invalid doctor or date selected." });
            }

            try
            {
                var availableSlots = await _context.AvailabilitySlots
                    .Where(s => s.DoctorId == doctorId &&
                                  s.Date == appointmentDate.Date &&
                                  !s.IsBooked &&
                                  (appointmentDate.Date > DateTime.Today || s.StartTime > DateTime.Now.TimeOfDay)
                                  )
                    .OrderBy(s => s.StartTime)
                    .Select(s => new SelectListItem
                    {
                        Value = s.AvailabilitySlotId.ToString(),
                        Text = $"{s.StartTime:hh\\:mm tt} - {s.EndTime:hh\\:mm tt}"
                    })
                    .ToListAsync();

                if (!availableSlots.Any())
                {
                    return Json(new { success = true, slots = new List<SelectListItem>(), message = "No available slots for the selected doctor and date." });
                }
                return Json(new { success = true, slots = availableSlots });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching available slots for Doctor ID {DoctorId} on Date {AppointmentDate}", doctorId, appointmentDate);
                return Json(new { success = false, message = "An error occurred while fetching available slots." });
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
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model, null, null);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            int parsedSelectedSlotId = 0; // Initialize to a default/invalid value
            bool slotIdParsedSuccessfully = false;

            if (string.IsNullOrWhiteSpace(model.SelectedAvailabilitySlotId) ||
                !int.TryParse(model.SelectedAvailabilitySlotId, out parsedSelectedSlotId))
            {
                ModelState.AddModelError("SelectedAvailabilitySlotId", "Please select an available time slot.");
            }
            else
            {
                slotIdParsedSuccessfully = true;
            }

            if (!ModelState.IsValid)
            {
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model,
                    slotIdParsedSuccessfully ? model.DoctorId : (int?)null,
                    slotIdParsedSuccessfully ? model.AppointmentDate : (DateTime?)null);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            // Now that ModelState is valid up to this point, and slotIdParsedSuccessfully is true,
            // parsedSelectedSlotId holds the valid integer.
            if (!slotIdParsedSuccessfully) // This is a defensive check; !ModelState.IsValid should catch it.
            {
                _logger.LogWarning("Slot ID parsing failed despite ModelState being valid. SelectedAvailabilitySlotId: {SelectedSlotIdValue}", model.SelectedAvailabilitySlotId);
                ModelState.AddModelError("SelectedAvailabilitySlotId", "Invalid slot selection process."); // Add a model error
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model, model.DoctorId, model.AppointmentDate);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            var chosenSlot = await _context.AvailabilitySlots
                                     .FirstOrDefaultAsync(s => s.AvailabilitySlotId == parsedSelectedSlotId &&
                                                               s.DoctorId == model.DoctorId &&
                                                               s.Date == model.AppointmentDate.Date &&
                                                               !s.IsBooked);

            if (chosenSlot == null)
            {
                ModelState.AddModelError("", "The selected time slot is no longer available or is invalid. Please refresh and try again.");
                TempData["BookingErrorMessage"] = "The selected time slot is no longer available. Please select another.";
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model, model.DoctorId, model.AppointmentDate);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            if (chosenSlot.Date == DateTime.Today && chosenSlot.StartTime <= DateTime.Now.TimeOfDay)
            {
                ModelState.AddModelError("", "The selected time slot has already passed for today. Please select a future time.");
                TempData["BookingErrorMessage"] = "The selected time slot has passed. Please select another.";
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model, model.DoctorId, model.AppointmentDate);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            DateTime newAppointmentStartTime = chosenSlot.Date.Add(chosenSlot.StartTime);
            DateTime newAppointmentEndTime = chosenSlot.Date.Add(chosenSlot.EndTime);

            bool patientHasConflict = await _context.Appointments
                .AnyAsync(a => a.PatientId == patientIdFromSession.Value &&
                               a.AppointmentDateTime < newAppointmentEndTime &&
                               GetAppointmentEndDateTime(a) > newAppointmentStartTime &&
                               a.Status != "Cancelled" && a.Status != "Completed");

            if (patientHasConflict)
            {
                ModelState.AddModelError("", $"You already have an overlapping appointment scheduled around this time.");
                TempData["BookingErrorMessage"] = "You have an existing overlapping appointment.";
            }

            if (!ModelState.IsValid)
            {
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model, model.DoctorId, model.AppointmentDate);
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
                // chosenSlot.BookedByAppointmentId will be null initially.
                // If you want a two-way link where AvailabilitySlot also knows the AppointmentId directly,
                // you'd set chosenSlot.BookedByAppointmentId = newAppointment.AppointmentId AFTER the first SaveChangesAsync
                // and then call SaveChangesAsync again. For now, the link from Appointment to Slot is primary.

                _context.Appointments.Add(newAppointment);
                _context.AvailabilitySlots.Update(chosenSlot);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment booked: PatientID {patientIdFromSession.Value}, SlotID {chosenSlot.AvailabilitySlotId}, DateTime {newAppointmentStartTime}");
                TempData["SuccessMessage"] = $"Appointment with Dr. {(_context.Doctors.Find(model.DoctorId)?.Name)} on {newAppointmentStartTime:MMMM dd, yyyy 'at' hh:mm tt} has been successfully requested.";
                return RedirectToAction("PatientDashboard");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error booking appointment. Slot ID {SlotId} might have been booked simultaneously.", chosenSlot.AvailabilitySlotId);
                ModelState.AddModelError("", "This time slot was just booked by someone else. Please select a different slot.");
                TempData["BookingErrorMessage"] = "This time slot was just booked. Please try another.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error booking appointment. Patient ID {PatientId}, Slot ID {SlotId}", patientIdFromSession.Value, chosenSlot?.AvailabilitySlotId);
                ModelState.AddModelError("", "A database error occurred. Please try again. If the problem persists, contact support.");
                TempData["BookingErrorMessage"] = "A database error occurred while booking your appointment.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error booking appointment. Patient ID {PatientId}, Slot ID {SlotId}", patientIdFromSession.Value, chosenSlot?.AvailabilitySlotId);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                TempData["BookingErrorMessage"] = "An unexpected error occurred while booking your appointment.";
            }

            await RepopulateBookAppointmentViewModelForPostErrorAsync(model, model.DoctorId, model.AppointmentDate);
            return View("~/Views/Home/BookAppointment.cshtml", model);
        }

        private DateTime GetAppointmentEndDateTime(Appointment appointment)
        {
            // If you add EndDateTime to your Appointment model, use it:
            // return appointment.EndDateTime;

            // If appointment is linked to an AvailabilitySlot, use its duration
            if (appointment.BookedAvailabilitySlotId.HasValue && appointment.BookedAvailabilitySlot != null)
            {
                return appointment.BookedAvailabilitySlot.Date.Add(appointment.BookedAvailabilitySlot.EndTime);
            }
            // Otherwise, assume a default duration, e.g., 1 hour for older or manually entered appointments
            return appointment.AppointmentDateTime.AddHours(1);
        }


        private async Task RepopulateBookAppointmentViewModelForPostErrorAsync(BookAppointmentViewModel model, int? preselectedDoctorId = null, DateTime? preselectedDate = null)
        {
            var doctors = await _context.Doctors
                                    .OrderBy(d => d.Name)
                                    .Select(d => new { d.DoctorId, NameAndSpec = $"{d.Name} ({d.Specialization})" })
                                    .ToListAsync();
            model.DoctorsList = new SelectList(doctors, "DoctorId", "NameAndSpec", model.DoctorId); // Use model.DoctorId for current selection

            if (preselectedDoctorId.HasValue && preselectedDoctorId.Value > 0 && preselectedDate.HasValue)
            {
                try
                {
                    model.AvailableTimeSlots = await _context.AvailabilitySlots
                        .Where(s => s.DoctorId == preselectedDoctorId.Value &&
                                      s.Date == preselectedDate.Value.Date &&
                                      !s.IsBooked &&
                                      (preselectedDate.Value.Date > DateTime.Today || s.StartTime > DateTime.Now.TimeOfDay))
                        .OrderBy(s => s.StartTime)
                        .Select(s => new SelectListItem
                        {
                            Value = s.AvailabilitySlotId.ToString(),
                            Text = $"{s.StartTime:hh\\:mm tt} - {s.EndTime:hh\\:mm tt}"
                        })
                        .ToListAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error repopulating available slots during POST error handling.");
                    model.AvailableTimeSlots = new List<SelectListItem>(); // Default to empty on error
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
    }
}