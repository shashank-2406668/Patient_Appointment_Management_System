// File: Patient_Appointment_Management_System/Controllers/DoctorController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // For EF Core operations
using Patient_Appointment_Management_System.Data; // Your DbContext namespace
using Patient_Appointment_Management_System.Models; // Your EF Core Models (Doctor)
using Patient_Appointment_Management_System.Utils; // For PasswordHelper
using Patient_Appointment_Management_System.ViewModels; // Your ViewModels
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks; // For asynchronous operations

namespace Patient_Appointment_Management_System.Controllers
{
    public class DoctorController : Controller
    {
        private readonly PatientAppointmentDbContext _context;
        private readonly ILogger<DoctorController> _logger;

        // Constructor to inject DbContext and Logger
        public DoctorController(PatientAppointmentDbContext context, ILogger<DoctorController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // === DOCTOR REGISTRATION ===
        [HttpGet]
        public IActionResult DoctorRegister()
        {
            return View("~/Views/Home/DoctorRegister.cshtml", new DoctorRegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorRegister(DoctorRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Check if email already exists in the database
                bool emailExists = await _context.Doctors.AnyAsync(d => d.Email == model.Email);
                if (emailExists)
                {
                    ModelState.AddModelError("Email", "This email address is already registered for a doctor.");
                    return View("~/Views/Home/DoctorRegister.cshtml", model);
                }

                // 2. Create a new Doctor ENTITY (from Models namespace)
                var doctor = new Doctor // Using Models.Doctor
                {
                    Name = model.Name,
                    Email = model.Email,
                    PasswordHash = PasswordHelper.HashPassword(model.Password), // HASH THE PASSWORD
                    Specialization = model.Specialization,
                    Phone = model.CountryCode + model.PhoneNumber // Combine country code and phone
                    // Other properties like Bio, YearsOfExperience, Address can be updated via Profile later
                };

                // 3. Add to DbContext and Save to Database
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Doctor registered successfully: {doctor.Email}");
                TempData["DoctorRegisterSuccessMessage"] = "Registration successful! Please log in.";
                return RedirectToAction("DoctorLogin");
            }
            // If ModelState is invalid, return the view with the model to show validation errors
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
                // 1. Find doctor by email in the database
                var doctorUser = await _context.Doctors
                                            .FirstOrDefaultAsync(d => d.Email == model.Email);

                // 2. Check if user exists and if password is correct
                if (doctorUser != null && PasswordHelper.VerifyPassword(model.Password, doctorUser.PasswordHash))
                {
                    // 3. Set session variables
                    HttpContext.Session.SetString("DoctorLoggedIn", "true");
                    HttpContext.Session.SetInt32("DoctorId", doctorUser.DoctorId); // Use actual DB ID
                    HttpContext.Session.SetString("DoctorName", doctorUser.Name);
                    HttpContext.Session.SetString("UserRole", "Doctor");

                    _logger.LogInformation($"Doctor login successful: {doctorUser.Email}");
                    TempData["SuccessMessage"] = "Login successful!"; // This will be shown on the Dashboard
                    return RedirectToAction("Dashboard"); // Redirect to Doctor's Dashboard
                }
                else
                {
                    _logger.LogWarning($"Doctor login failed for email: {model.Email}");
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                }
            }
            // If model is null or ModelState is invalid after checks
            return View("~/Views/Home/DoctorLogin.cshtml", model);
        }

        // === DOCTOR DASHBOARD & ACTIONS ===
        private bool IsDoctorLoggedIn()
        {
            return HttpContext.Session.GetString("DoctorLoggedIn") == "true" &&
                   HttpContext.Session.GetString("UserRole") == "Doctor";
        }

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
                _logger.LogWarning($"Doctor with ID {doctorId.Value} not found in database during dashboard access. Clearing session.");
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Your account could not be found. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }
            var doctorName = doctor.Name;


            // Fetch today's appointments for the logged-in doctor
            var todaysAppointments = await _context.Appointments
                .Include(a => a.Patient) // Include Patient details
                .Where(a => a.DoctorId == doctorId.Value && a.AppointmentDateTime.Date == DateTime.Today)
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new AppointmentSummaryViewModel
                {
                    Id = a.AppointmentId,
                    PatientName = a.Patient.Name, // Assuming Patient model has a Name property
                    AppointmentDateTime = a.AppointmentDateTime,
                    Status = a.Status,
                    // You might want to add Issue or other relevant details
                })
                .ToListAsync();

            // Fetch notifications for the doctor (example)
            // var notifications = await _context.Notifications
            //     .Where(n => n.DoctorId == doctorId.Value && !n.IsRead)
            //     .OrderByDescending(n => n.SentDate)
            //     .Take(5) // Get latest 5 unread notifications
            //     .Select(n => n.Message) // Or map to a NotificationViewModel
            //     .ToListAsync();

            var viewModel = new DoctorDashboardViewModel
            {
                DoctorDisplayName = $"Dr. {doctorName}",
                TodaysAppointments = todaysAppointments,
                // Notifications = notifications // Or a list of strings
            };
            viewModel.Notifications.Add("Welcome to your dashboard!"); // Example static notification

            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View("~/Views/Home/DoctorDashboard.cshtml", viewModel);
        }

        // === DOCTOR PROFILE ACTIONS ===
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to view your profile.";
                return RedirectToAction("DoctorLogin");
            }

            var doctorIdFromSession = HttpContext.Session.GetInt32("DoctorId");
            if (doctorIdFromSession == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var doctor = await _context.Doctors.FindAsync(doctorIdFromSession.Value);
            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Your profile could not be found. Please log in again.";
                HttpContext.Session.Clear();
                return RedirectToAction("DoctorLogin");
            }

            // For DoctorProfileViewModel, you might need to add Bio, YearsOfExperience, Address to your Doctor Model
            // or adjust DoctorProfileViewModel to only include fields present in the Doctor Model.
            // For now, we'll map what's available.
            var doctorProfileViewModel = new DoctorProfileViewModel
            {
                Id = doctor.DoctorId,
                Name = doctor.Name,
                Email = doctor.Email, // Email is usually not updatable via general profile edit
                Specialization = doctor.Specialization,
                // Assuming Phone in DB is combined CountryCode + PhoneNumber
                // This is a basic split, improve as needed.
                CountryCode = "", // Default
                PhoneNumber = doctor.Phone   // Default
            };

            if (!string.IsNullOrEmpty(doctor.Phone))
            {
                // Attempt to split phone into country code and number
                // This is a simplistic approach, consider a library or more robust parsing if complex codes are used.
                if (doctor.Phone.StartsWith("+"))
                {
                    int firstDigitIndex = -1;
                    for (int i = 1; i < doctor.Phone.Length; i++)
                    {
                        if (char.IsDigit(doctor.Phone[i]))
                        {
                            firstDigitIndex = i;
                            break;
                        }
                    }

                    if (firstDigitIndex > 1) // Found digits after '+'
                    {
                        // Look for first non-digit after '+' to determine end of country code
                        int codeEndIndex = firstDigitIndex;
                        while (codeEndIndex < doctor.Phone.Length && char.IsDigit(doctor.Phone[codeEndIndex]))
                        {
                            codeEndIndex++;
                        }
                        // A common pattern: +CC NNNNNNNNNN or +C NNNNNNNNNN
                        // Let's assume up to 3 digits for country code part after '+'
                        int potentialCodeLength = Math.Min(3, codeEndIndex - 1); // Max 3 digits for code part
                        if (doctor.Phone.Length > potentialCodeLength + 1)
                        {
                            doctorProfileViewModel.CountryCode = doctor.Phone.Substring(0, potentialCodeLength + 1);
                            doctorProfileViewModel.PhoneNumber = doctor.Phone.Substring(potentialCodeLength + 1);
                        }

                    }
                }
            }


            if (TempData["ProfileSuccessMessage"] != null) ViewBag.SuccessMessage = TempData["ProfileSuccessMessage"];
            if (TempData["ProfileErrorMessage"] != null) ViewBag.ErrorMessage = TempData["ProfileErrorMessage"];

            return View("~/Views/Home/DoctorProfile.cshtml", doctorProfileViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(DoctorProfileViewModel model)
        {
            if (!IsDoctorLoggedIn())
            {
                return RedirectToAction("DoctorLogin");
            }

            var doctorIdFromSession = HttpContext.Session.GetInt32("DoctorId");
            if (doctorIdFromSession == null || model.Id != doctorIdFromSession.Value)
            {
                TempData["ProfileErrorMessage"] = "Unauthorized profile update attempt or session issue.";
                return RedirectToAction("Profile");
            }

            var doctorToUpdate = await _context.Doctors.FindAsync(model.Id);
            if (doctorToUpdate == null)
            {
                TempData["ProfileErrorMessage"] = "Profile not found for update.";
                return RedirectToAction("Profile");
            }

            // Ensure email from model matches stored email if it's not supposed to be changed here.
            // If email change is allowed, add validation for uniqueness.
            // For now, we assume DoctorProfile.cshtml has Email as readonly or it's not changed.
            // ModelState.Remove("Email"); // If email is not part of the editable form in the ViewModel

            if (ModelState.IsValid)
            {
                doctorToUpdate.Name = model.Name;
                doctorToUpdate.Specialization = model.Specialization;
                doctorToUpdate.Phone = model.CountryCode + model.PhoneNumber;
                // Update other fields if they exist in your Doctor model and DoctorProfileViewModel
                // doctorToUpdate.Bio = model.Bio;
                // doctorToUpdate.YearsOfExperience = model.YearsOfExperience;
                // doctorToUpdate.Address = model.Address;

                try
                {
                    _context.Doctors.Update(doctorToUpdate);
                    await _context.SaveChangesAsync();

                    HttpContext.Session.SetString("DoctorName", doctorToUpdate.Name);
                    TempData["ProfileSuccessMessage"] = "Profile updated successfully!";
                    _logger.LogInformation($"Doctor profile updated for ID: {doctorToUpdate.DoctorId}");
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, $"Concurrency error updating profile for Doctor ID: {model.Id}");
                    TempData["ProfileErrorMessage"] = "The profile was modified by another user. Please refresh and try again.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error updating profile for Doctor ID: {model.Id}");
                    TempData["ProfileErrorMessage"] = "An unexpected error occurred while updating your profile.";
                }
                return RedirectToAction("Profile");
            }

            // If ModelState is invalid, return the view with the model to show validation errors
            TempData["ProfileErrorMessage"] = "Please correct the validation errors and try again.";
            return View("~/Views/Home/DoctorProfile.cshtml", model);
        }


        // === AVAILABILITY ACTIONS ===
        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to manage availability.";
                return RedirectToAction("DoctorLogin");
            }
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var existingSlotsFromDb = await _context.AvailabilitySlots
                .Where(s => s.DoctorId == doctorId.Value && s.Date >= DateTime.Today) // Show future or today's slots
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new ExistingAvailabilitySlotViewModel // Map to ViewModel
                {
                    Id = s.AvailabilitySlotId,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsBooked = s.IsBooked,
                    // PatientNameIfBooked = s.IsBooked && s.BookedByAppointment != null ? s.BookedByAppointment.Patient.Name : null
                    // Requires navigation property BookedByAppointment and then Patient.Name
                    // For simplicity, we might omit PatientNameIfBooked here or handle it differently
                })
                .ToListAsync();

            var viewModel = new DoctorManageAvailabilityViewModel
            {
                ExistingSlots = existingSlotsFromDb,
                NewSlot = new AvailabilitySlotInputViewModel { Date = DateTime.Today.AddDays(1) } // Default for new slot
            };

            if (TempData["AvailabilitySuccessMessage"] != null) ViewBag.SuccessMessage = TempData["AvailabilitySuccessMessage"];
            if (TempData["AvailabilityErrorMessage"] != null) ViewBag.ErrorMessage = TempData["AvailabilityErrorMessage"];

            return View("~/Views/Home/DoctorManageAvailability.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(AvailabilitySlotInputViewModel newSlotInput)
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to add availability.";
                return RedirectToAction("DoctorLogin");
            }
            var doctorIdFromSession = HttpContext.Session.GetInt32("DoctorId");
            if (doctorIdFromSession == null)
            {
                TempData["AvailabilityErrorMessage"] = "Session error. Cannot add availability.";
                // To avoid losing user input, we should repopulate and return to the view
                // However, without a doctorId, adding availability is problematic.
                // Redirecting to login or availability GET might be better.
                return RedirectToAction("Availability");
            }

            if (ModelState.IsValid)
            {
                if (newSlotInput.EndTime <= newSlotInput.StartTime)
                {
                    ModelState.AddModelError("NewSlot.EndTime", "End time must be after start time.");
                }
                if (newSlotInput.Date < DateTime.Today)
                {
                    ModelState.AddModelError("NewSlot.Date", "Availability date cannot be in the past.");
                }
                // Check for overlapping slots for this doctor
                bool overlaps = await _context.AvailabilitySlots.AnyAsync(s =>
                    s.DoctorId == doctorIdFromSession.Value &&
                    s.Date == newSlotInput.Date &&
                    newSlotInput.StartTime < s.EndTime && // New slot starts before existing one ends
                    newSlotInput.EndTime > s.StartTime    // New slot ends after existing one starts
                );
                if (overlaps)
                {
                    ModelState.AddModelError("NewSlot.StartTime", "This time slot overlaps with an existing one.");
                }


                if (ModelState.IsValid)
                {
                    var availabilitySlot = new AvailabilitySlot
                    {
                        DoctorId = doctorIdFromSession.Value,
                        Date = newSlotInput.Date,
                        StartTime = newSlotInput.StartTime,
                        EndTime = newSlotInput.EndTime,
                        IsBooked = false
                    };
                    _context.AvailabilitySlots.Add(availabilitySlot);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Availability slot added for Doctor ID {doctorIdFromSession.Value} on {newSlotInput.Date} from {newSlotInput.StartTime} to {newSlotInput.EndTime}");
                    TempData["AvailabilitySuccessMessage"] = "Availability slot added successfully!";
                    return RedirectToAction("Availability");
                }
            }

            TempData["AvailabilityErrorMessage"] = "Failed to add availability slot. Please check the details.";
            // Repopulate the full view model if returning to the view with errors
            var existingSlotsFromDb = await _context.AvailabilitySlots
               .Where(s => s.DoctorId == doctorIdFromSession.Value && s.Date >= DateTime.Today)
               .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
               .Select(s => new ExistingAvailabilitySlotViewModel
               {
                   Id = s.AvailabilitySlotId,
                   Date = s.Date,
                   StartTime = s.StartTime,
                   EndTime = s.EndTime,
                   IsBooked = s.IsBooked
               })
               .ToListAsync();

            var fullViewModel = new DoctorManageAvailabilityViewModel
            {
                ExistingSlots = existingSlotsFromDb,
                NewSlot = newSlotInput // Pass back the input with its errors
            };
            return View("~/Views/Home/DoctorManageAvailability.cshtml", fullViewModel);
        }


        // === DOCTOR FORGOT PASSWORD ===
        [HttpGet]
        public IActionResult DoctorForgotPassword()
        {
            return View("~/Views/Home/DoctorForgotPassword.cshtml", new DoctorForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorForgotPassword(DoctorForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var doctorExists = await _context.Doctors.AnyAsync(d => d.Email == model.Email);
                if (doctorExists)
                {
                    _logger.LogInformation($"Password reset initiated for existing doctor email: {model.Email}");
                    // TODO: Implement actual password reset logic (generate token, send email, store token with expiry)
                }
                else
                {
                    _logger.LogInformation($"Password reset attempt for non-existent doctor email: {model.Email}");
                }
                // Always show the same message to prevent email enumeration.
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";
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
            TempData["DoctorLogoutMessage"] = "You have been successfully logged out."; // Specific to doctor logout
            return RedirectToAction("Index", "Home"); // Or DoctorLogin
        }
    }
}