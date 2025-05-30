// File: Patient_Appointment_Management_System/Controllers/PatientController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // For EF Core operations like ToListAsync, FirstOrDefaultAsync
using Patient_Appointment_Management_System.Data; // Your DbContext namespace
using Patient_Appointment_Management_System.Models; // Your EF Core Models (Patient, Appointment etc.)
using Patient_Appointment_Management_System.Utils; // For PasswordHelper
using Patient_Appointment_Management_System.ViewModels; // Your ViewModels
using System.Diagnostics;
using Microsoft.AspNetCore.Http; // For HttpContext.Session
using System;
using System.Linq;
using System.Threading.Tasks; // For asynchronous operations

namespace Patient_Appointment_Management_System.Controllers
{
    public class PatientController : Controller
    {
        private readonly PatientAppointmentDbContext _context; // Inject the DbContext
        private readonly ILogger<PatientController> _logger; // Optional: for logging

        // Constructor to inject DbContext and Logger
        public PatientController(PatientAppointmentDbContext context, ILogger<PatientController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Removed: static List<PatientProfileViewModel> _patientProfiles and _nextPatientId

        // === PATIENT REGISTRATION ===
        [HttpGet]
        public IActionResult PatientRegister()
        {
            // The view path is inferred if it's in Views/Patient/PatientRegister.cshtml
            // If it's in Views/Home/, keep the explicit path.
            return View("~/Views/Home/PatientRegister.cshtml", new PatientRegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientRegister(PatientRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Check if email already exists in the database
                bool emailExists = await _context.Patients.AnyAsync(p => p.Email == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "This email address is already registered.");
                    return View("~/Views/Home/PatientRegister.cshtml", model);
                }

                // 2. Create a new Patient ENTITY (from Models namespace)
                var patient = new Patient // Using Models.Patient
                {
                    Name = model.Name,
                    Email = model.Email,
                    PasswordHash = PasswordHelper.HashPassword(model.Password), // HASH THE PASSWORD
                    Phone = model.CountryCode + model.PhoneNumber, // Combine if needed
                    Dob = model.Dob,
                    Address = model.Address
                };

                // 3. Add to DbContext and Save to Database
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
            if (TempData["ForgotPasswordMessage"] != null) ViewBag.InfoMessage = TempData["ForgotPasswordMessage"]; // For forgot password message

            return View("~/Views/Home/PatientLogin.cshtml", new PatientLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientLogin(PatientLoginViewModel model)
        {
            if (ModelState.IsValid) // ModelState.IsValid checks [Required], [EmailAddress] etc. from ViewModel
            {
                // 1. Find patient by email in the database
                var patientUser = await _context.Patients
                                            .FirstOrDefaultAsync(p => p.Email == model.Email);

                // 2. Check if user exists and if password is correct
                if (patientUser != null && PasswordHelper.VerifyPassword(model.Password, patientUser.PasswordHash))
                {
                    // 3. Set session variables
                    HttpContext.Session.SetString("PatientLoggedIn", "true");
                    HttpContext.Session.SetInt32("PatientId", patientUser.PatientId); // Use actual DB ID
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
            // If model is null or ModelState is invalid after checks
            return View("~/Views/Home/PatientLogin.cshtml", model);
        }

        // === HELPER & OTHER PATIENT ACTIONS ===
        private bool IsPatientLoggedIn() // This remains the same
        {
            return HttpContext.Session.GetString("PatientLoggedIn") == "true" &&
                   HttpContext.Session.GetString("UserRole") == "Patient";
        }

        [HttpGet]
        public IActionResult PatientDashboard() // Mostly remains the same, just session check
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "You need to log in as a patient to access the dashboard.";
                return RedirectToAction("PatientLogin");
            }
            var patientName = HttpContext.Session.GetString("PatientName") ?? "User";
            ViewBag.WelcomeMessage = $"Welcome, {patientName}!";
            if (TempData["GlobalSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["GlobalSuccessMessage"];

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

            // Fetch patient from database using the ID stored in session
            var patient = await _context.Patients.FindAsync(patientIdFromSession.Value);

            if (patient == null)
            {
                _logger.LogError($"Patient profile not found for ID: {patientIdFromSession.Value}. Clearing session.");
                TempData["ErrorMessage"] = "Your profile could not be found. Please log in again.";
                HttpContext.Session.Clear();
                return RedirectToAction("PatientLogin");
            }

            // Map ENTITY to ViewModel for display (PatientProfileViewModel might be different from Models.Patient)
            // Assuming PatientProfileViewModel is what your View expects and has similar properties.
            // If PatientProfileViewModel has properties like CountryCode, you'll need to handle that (e.g., split Phone).
            var patientProfileViewModel = new PatientProfileViewModel
            {
                Id = patient.PatientId, // Use PatientId from the entity
                Name = patient.Name,
                Email = patient.Email, // Email usually isn't updatable on profile page, so display it
                Phone = patient.Phone, // You might need to split this if your VM has CountryCode & PhoneNumber separate
                Dob = patient.Dob,
                Address = patient.Address
                // Do NOT include PasswordHash here
            };

            if (TempData["ProfileUpdateMessage"] != null) ViewBag.SuccessMessage = TempData["ProfileUpdateMessage"];
            return View("~/Views/Home/PatientProfile.cshtml", patientProfileViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientProfile(PatientProfileViewModel model) // ViewModel received from form
        {
            if (!IsPatientLoggedIn()) return RedirectToAction("PatientLogin");

            var patientIdFromSession = HttpContext.Session.GetInt32("PatientId");
            if (patientIdFromSession == null || model.Id != patientIdFromSession.Value)
            {
                TempData["ErrorMessage"] = "Unauthorized profile update attempt or session mismatch.";
                HttpContext.Session.Clear(); // For security, clear session on mismatch
                return RedirectToAction("PatientLogin");
            }

            if (ModelState.IsValid) // Validates the ViewModel
            {
                // Fetch the existing patient entity from the database
                var patientToUpdate = await _context.Patients.FindAsync(model.Id);

                if (patientToUpdate != null)
                {
                    // Update only the allowed fields from the ViewModel
                    patientToUpdate.Name = model.Name;
                    patientToUpdate.Phone = model.Phone; // If Phone includes CountryCode, ensure it's handled.
                                                         // Or, if VM has CountryCode and PhoneNumber separate:
                                                         // patientToUpdate.Phone = model.CountryCode + model.PhoneNumber;
                    patientToUpdate.Dob = model.Dob;
                    patientToUpdate.Address = model.Address;
                    // DO NOT update Email from here unless it's a specific feature with verification.
                    // DO NOT update PasswordHash from this profile update form. That's a separate "Change Password" feature.

                    _context.Patients.Update(patientToUpdate); // Mark entity as modified
                    await _context.SaveChangesAsync();         // Save changes to database

                    // Update session if name changed, for display purposes
                    HttpContext.Session.SetString("PatientName", patientToUpdate.Name);
                    _logger.LogInformation($"Patient profile updated for ID: {patientToUpdate.PatientId}");
                    TempData["ProfileUpdateMessage"] = "Profile updated successfully!";
                }
                else
                {
                    _logger.LogWarning($"Attempted to update non-existent patient profile. ID from model: {model.Id}");
                    TempData["ProfileUpdateMessage"] = "Error: Profile not found for update."; // More user-friendly: TempData["ErrorMessage"]
                }
                return RedirectToAction("PatientProfile"); // Redirect back to GET to show updated profile and message
            }

            // If ModelState is invalid, return the view with the model to show validation errors
            // The model passed back should be the ViewModel with the user's input and errors
            _logger.LogWarning($"Patient profile update failed due to ModelState errors for ID: {model.Id}");
            return View("~/Views/Home/PatientProfile.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientLogout() // Remains the same as it's session-based
        {
            try
            {
                HttpContext.Session.Clear();
                _logger.LogInformation("Patient logged out successfully.");
                TempData["GlobalSuccessMessage"] = "You have been successfully logged out.";
                return RedirectToAction("Index", "Home"); // Or PatientLogin
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
        public IActionResult BookAppointment() // Session check remains
        {
            if (!IsPatientLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to book an appointment.";
                return RedirectToAction("PatientLogin");
            }
            // TODO: This view might need data like a list of doctors, available slots, etc.
            // You'll fetch this from the DB and pass it via a ViewModel.
            // For now, just returning the view.
            return View("~/Views/Home/BookAppointment.cshtml");
        }

        // TODO: Implement the POST action for booking an appointment (will involve DB operations)
        // Example structure:
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(BookAppointmentViewModel model) // ViewModel for booking form
        {
            if (!IsPatientLoggedIn())
            {
                return RedirectToAction("PatientLogin");
            }

            var patientIdFromSession = HttpContext.Session.GetInt32("PatientId");
            if (patientIdFromSession == null)
            {
                 TempData["ErrorMessage"] = "Session error. Please log in again.";
                 return RedirectToAction("PatientLogin");
            }

            if (ModelState.IsValid)
            {
                // 1. Validate DoctorId, Date, TimeSlot (e.g., is doctor available?)
                //    var doctor = await _context.Doctors.FindAsync(model.DoctorId);
                //    if (doctor == null) { ModelState.AddModelError("DoctorId", "Selected doctor not found."); }
                //
                //    var selectedSlot = await _context.AvailabilitySlots.FirstOrDefaultAsync(s =>
                //        s.DoctorId == model.DoctorId &&
                //        s.Date.Date == model.AppointmentDate.Date && // Compare Date part only
                //        s.StartTime == model.TimeSlot &&
                //        !s.IsBooked);
                //
                //    if (selectedSlot == null)
                //    {
                //        ModelState.AddModelError("", "The selected time slot is not available or does not exist.");
                //        // Repopulate any dropdowns needed for the view and return
                //        return View("~/Views/Home/BookAppointment.cshtml", model);
                //    }

                // 2. Create new Appointment ENTITY
                var newAppointment = new Appointment
                {
                    PatientId = patientIdFromSession.Value,
                    DoctorId = model.DoctorId, // Assuming BookAppointmentViewModel has DoctorId
                    AppointmentDateTime = model.AppointmentDate.Add(model.TimeSlot), // Combine Date and Time
                    Status = "Scheduled", // Initial status
                    Issue = model.Issue,
                    // BookedAvailabilitySlotId = selectedSlot.AvailabilitySlotId // Link to the slot
                };

                _context.Appointments.Add(newAppointment);
                // selectedSlot.IsBooked = true; // Mark the slot as booked
                // selectedSlot.BookedByAppointmentId = newAppointment.AppointmentId; // This will be set after newAppointment gets an ID
                // _context.AvailabilitySlots.Update(selectedSlot);

                await _context.SaveChangesAsync();
                
                // To set BookedByAppointmentId on the slot correctly after the appointment is saved:
                // selectedSlot.BookedByAppointmentId = newAppointment.AppointmentId; 
                // await _context.SaveChangesAsync();


                _logger.LogInformation($"Appointment booked for Patient ID {patientIdFromSession.Value} with Doctor ID {model.DoctorId}");
                TempData["SuccessMessage"] = "Appointment requested successfully! You will be notified upon confirmation.";
                return RedirectToAction("PatientDashboard");
            }

            // If ModelState is invalid, repopulate necessary data for the view (e.g., doctor list)
            // ViewBag.Doctors = new SelectList(await _context.Doctors.ToListAsync(), "DoctorId", "Name");
            return View("~/Views/Home/BookAppointment.cshtml", model);
        }
        */

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
                if (patientExists)
                {
                    _logger.LogInformation($"Password reset initiated for existing patient email: {model.Email}");
                    // TODO: Actual password reset logic (generate token, send email)
                }
                else
                {
                    _logger.LogInformation($"Password reset attempt for non-existent patient email: {model.Email}");
                }
                // Always show the same message to prevent email enumeration.
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";
                return RedirectToAction("PatientLogin");
            }
            return View("~/Views/Home/PatientForgotPassword.cshtml", model);
        }

        // TODO: Add PatientResetPassword GET and POST actions
    }
}