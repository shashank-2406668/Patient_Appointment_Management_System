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
                .Where(a => a.PatientId == patientId.Value && a.AppointmentDateTime >= now)
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
                .Where(a => a.PatientId == patientId.Value && a.AppointmentDateTime < now)
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

        // === BOOK APPOINTMENT ACTIONS ===
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
                AppointmentDate = DateTime.Today // Default to today
            };
            return View("~/Views/Home/BookAppointment.cshtml", viewModel);
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
                TempData["ErrorMessage"] = "Session error. Please log in again to book an appointment.";
                await RepopulateBookAppointmentViewModelAsync(model);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            DateTime parsedStartTime = default;
            DateTime parsedEndTime = default;
            bool timeSlotValidlyParsed = false;

            if (string.IsNullOrWhiteSpace(model.TimeSlot))
            {
                ModelState.AddModelError("TimeSlot", "Time slot must be selected.");
            }
            else
            {
                string[] timeParts = model.TimeSlot.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
                if (timeParts.Length != 2)
                {
                    ModelState.AddModelError("TimeSlot", "Invalid time slot format. Expected 'StartTime - EndTime'.");
                }
                else
                {
                    bool startTimeParsed = DateTime.TryParseExact(timeParts[0].Trim(),
                                               new[] { "hh:mm tt", "h:mm tt" },
                                               CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedStartTime);
                    bool endTimeParsed = DateTime.TryParseExact(timeParts[1].Trim(),
                                               new[] { "hh:mm tt", "h:mm tt" },
                                               CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedEndTime);

                    if (!startTimeParsed)
                    {
                        ModelState.AddModelError("TimeSlot", "Invalid start time format in selected slot.");
                    }
                    if (!endTimeParsed)
                    {
                        ModelState.AddModelError("TimeSlot", "Invalid end time format in selected slot.");
                    }

                    if (startTimeParsed && endTimeParsed)
                    {
                        if (parsedEndTime.TimeOfDay <= parsedStartTime.TimeOfDay)
                        {
                            ModelState.AddModelError("TimeSlot", "End time in slot must be after start time.");
                        }
                        else
                        {
                            timeSlotValidlyParsed = true;
                        }
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                await RepopulateBookAppointmentViewModelAsync(model);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            if (!timeSlotValidlyParsed)
            {
                _logger.LogWarning("Time slot parsing failed logic path. TimeSlot: {TimeSlot}", model.TimeSlot);
                // ModelState.AddModelError("TimeSlot", "The selected time slot could not be processed."); // Optionally add a generic error
                await RepopulateBookAppointmentViewModelAsync(model);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            DateTime appointmentStartDateTime = model.AppointmentDate.Date.Add(parsedStartTime.TimeOfDay);
            DateTime appointmentEndDateTime = model.AppointmentDate.Date.Add(parsedEndTime.TimeOfDay);

            bool patientHasConflict = await _context.Appointments
                .AnyAsync(a => a.PatientId == patientIdFromSession.Value &&
                               a.AppointmentDateTime < appointmentEndDateTime &&
                               a.AppointmentDateTime.AddHours(1) > appointmentStartDateTime &&
                               a.Status != "Cancelled" && a.Status != "Completed");

            if (patientHasConflict)
            {
                ModelState.AddModelError("", $"You already have an overlapping appointment around {appointmentStartDateTime:hh:mm tt} on {model.AppointmentDate:MMMM dd, yyyy}.");
            }

            bool doctorHasConflict = await _context.Appointments
                .AnyAsync(a => a.DoctorId == model.DoctorId &&
                               a.AppointmentDateTime < appointmentEndDateTime &&
                               a.AppointmentDateTime.AddHours(1) > appointmentStartDateTime &&
                               a.Status != "Cancelled");

            if (doctorHasConflict)
            {
                ModelState.AddModelError("", $"The selected doctor is not available for the period {appointmentStartDateTime:hh:mm tt} - {appointmentEndDateTime:hh:mm tt} on {model.AppointmentDate:MMMM dd, yyyy}. Please choose another time or doctor.");
            }

            if (!ModelState.IsValid)
            {
                await RepopulateBookAppointmentViewModelAsync(model);
                return View("~/Views/Home/BookAppointment.cshtml", model);
            }

            try
            {
                var newAppointment = new Appointment
                {
                    PatientId = patientIdFromSession.Value,
                    DoctorId = model.DoctorId,
                    AppointmentDateTime = appointmentStartDateTime,
                    Status = "Scheduled",
                    Issue = model.Issue
                };

                _context.Appointments.Add(newAppointment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Appointment booked: PatientID {patientIdFromSession.Value}, DoctorID {model.DoctorId}, DateTime {appointmentStartDateTime}");
                TempData["SuccessMessage"] = $"Appointment successfully requested for {appointmentStartDateTime:MMMM dd, yyyy 'at' hh:mm tt}.";
                return RedirectToAction("PatientDashboard");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error booking appointment for Patient ID {PatientId}", patientIdFromSession.Value);
                ModelState.AddModelError("", "A database error occurred. Please try again. If the problem persists, contact support.");
                TempData["BookingErrorMessage"] = "A database error occurred while booking your appointment.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking appointment for Patient ID {PatientId}", patientIdFromSession.Value);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again.");
                TempData["BookingErrorMessage"] = "An unexpected error occurred while booking your appointment.";
            }

            await RepopulateBookAppointmentViewModelAsync(model);
            return View("~/Views/Home/BookAppointment.cshtml", model);
        }

        private async Task RepopulateBookAppointmentViewModelAsync(BookAppointmentViewModel model)
        {
            var doctors = await _context.Doctors
                                    .OrderBy(d => d.Name)
                                    .Select(d => new { d.DoctorId, NameAndSpec = $"{d.Name} ({d.Specialization})" })
                                    .ToListAsync();
            model.DoctorsList = new SelectList(doctors, "DoctorId", "NameAndSpec", model.DoctorId);
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