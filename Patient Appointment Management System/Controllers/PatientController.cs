// File: Patient_Appointment_Management_System/Controllers/PatientController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using Patient_Appointment_Management_System.ViewModels;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using System.Globalization; // For CultureInfo

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

        // ... (other actions like PatientRegister, PatientLogin, PatientDashboard, PatientProfile, PatientLogout remain the same) ...

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

        // === HELPER & OTHER PATIENT ACTIONS ===
        private bool IsPatientLoggedIn()
        {
            return HttpContext.Session.GetString("PatientLoggedIn") == "true" &&
                   HttpContext.Session.GetString("UserRole") == "Patient";
        }

        [HttpGet]
        public IActionResult PatientDashboard()
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "You need to log in as a patient to access the dashboard.";
                return RedirectToAction("PatientLogin");
            }
            var patientName = HttpContext.Session.GetString("PatientName") ?? "User";
            ViewBag.WelcomeMessage = $"Welcome, {patientName}!";
            if (TempData["GlobalSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["GlobalSuccessMessage"];
            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = (ViewBag.SuccessMessage != null ? ViewBag.SuccessMessage + "<br/>" : "") + TempData["SuccessMessage"];


            return View("~/Views/Home/PatientDashboard.cshtml");
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
                    _logger.LogWarning($"Attempted to update non-existent patient profile. ID from model: {model.Id}");
                    TempData["ProfileUpdateMessage"] = "Error: Profile not found for update.";
                }
                return RedirectToAction("PatientProfile");
            }
            _logger.LogWarning($"Patient profile update failed due to ModelState errors for ID: {model.Id}");
            return View("~/Views/Home/PatientProfile.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientLogout()
        {
            try
            {
                HttpContext.Session.Clear();
                _logger.LogInformation("Patient logged out successfully.");
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

            // Fetch doctors for the dropdown
            // Assuming you have a Doctor model with DoctorId and Name properties
            var doctors = await _context.Doctors
                                    .Select(d => new { d.DoctorId, d.Name, d.Specialization }) // Fetch only needed fields
                                    .ToListAsync();

            var viewModel = new BookAppointmentViewModel
            {
                DoctorsList = new SelectList(doctors, "DoctorId", "Name", null, "Specialization"), // Group by Specialization
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
                return RedirectToAction("PatientLogin");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Parse the TimeSlot to get the start time
                    // Example TimeSlot: "09:00 AM - 10:00 AM"
                    string timeSlotPart = model.TimeSlot.Split('-')[0].Trim(); // Gets "09:00 AM"
                    DateTime parsedTime;
                    if (!DateTime.TryParseExact(timeSlotPart, "hh:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedTime))
                    {
                        // Handle alternative format like "HH:mm" if AM/PM is missing or different
                        if (!DateTime.TryParseExact(timeSlotPart, "h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedTime))
                        {
                            ModelState.AddModelError("TimeSlot", "Invalid time slot format.");
                            // Repopulate DoctorsList if returning to view with errors
                            var doctors = await _context.Doctors.Select(d => new { d.DoctorId, d.Name, d.Specialization }).ToListAsync();
                            model.DoctorsList = new SelectList(doctors, "DoctorId", "Name", model.DoctorId, "Specialization");
                            return View("~/Views/Home/BookAppointment.cshtml", model);
                        }
                    }

                    DateTime appointmentDateTime = model.AppointmentDate.Date.Add(parsedTime.TimeOfDay);

                    // Optional: Check if the selected doctor exists (though SelectList should prevent invalid IDs)
                    var doctorExists = await _context.Doctors.AnyAsync(d => d.DoctorId == model.DoctorId);
                    if (!doctorExists)
                    {
                        ModelState.AddModelError("DoctorId", "Selected doctor is not valid.");
                        // Repopulate and return
                        var doctors = await _context.Doctors.Select(d => new { d.DoctorId, d.Name, d.Specialization }).ToListAsync();
                        model.DoctorsList = new SelectList(doctors, "DoctorId", "Name", model.DoctorId, "Specialization");
                        return View("~/Views/Home/BookAppointment.cshtml", model);
                    }

                    // Optional: Check for conflicts (e.g., doctor already booked, patient already booked)
                    // This can be complex and might involve checking an AvailabilitySlots table
                    // For now, we'll keep it simple.

                    var newAppointment = new Appointment
                    {
                        PatientId = patientIdFromSession.Value,
                        DoctorId = model.DoctorId,
                        AppointmentDateTime = appointmentDateTime,
                        Status = "Scheduled", // Initial status
                        Issue = model.Issue
                        // BookedAvailabilitySlotId = null; // If not directly using specific slots yet
                    };

                    _context.Appointments.Add(newAppointment);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"Appointment booked successfully for Patient ID {patientIdFromSession.Value} with Doctor ID {model.DoctorId} on {newAppointment.AppointmentDateTime}");
                    TempData["SuccessMessage"] = $"Appointment with Dr. {(_context.Doctors.Find(model.DoctorId)?.Name ?? "selected doctor")} on {appointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt} has been successfully requested.";
                    return RedirectToAction("PatientDashboard");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error booking appointment for Patient ID {PatientId}", patientIdFromSession.Value);
                    ModelState.AddModelError("", "An unexpected error occurred while booking the appointment. Please try again.");
                }
            }

            // If ModelState is invalid, repopulate necessary data for the view
            var allDoctors = await _context.Doctors.Select(d => new { d.DoctorId, d.Name, d.Specialization }).ToListAsync();
            model.DoctorsList = new SelectList(allDoctors, "DoctorId", "Name", model.DoctorId, "Specialization");
            // The AvailableTimeSlots list is already part of the model, so it will be re-rendered.
            return View("~/Views/Home/BookAppointment.cshtml", model);
        }


        // === PATIENT FORGOT PASSWORD (Initial DB check) ===
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
                // Business logic for sending reset link would go here
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";
                return RedirectToAction("PatientLogin");
            }
            return View("~/Views/Home/PatientForgotPassword.cshtml", model);
        }
    }
}