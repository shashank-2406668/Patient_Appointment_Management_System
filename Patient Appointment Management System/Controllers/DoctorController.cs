// File: Controllers/DoctorController.cs
using Microsoft.AspNetCore.Mvc;
using Patient_Appointment_Management_System.ViewModels; // For ViewModels
using System.Diagnostics;
using Microsoft.AspNetCore.Http; // Required for HttpContext.Session
using System;                      // Required for DateTime
using System.Collections.Generic;  // Required for List
using System.Linq;                 // Required for Linq operations like .FirstOrDefault()

namespace Patient_Appointment_Management_System.Controllers
{
    public class DoctorController : Controller
    {
        // Simulated data store for doctor profiles
        private static List<DoctorProfileViewModel> _doctorProfiles = new List<DoctorProfileViewModel>
        {
            new DoctorProfileViewModel { Id = 1, Email = "doctor@example.com", Name = "Example", Specialization = "General Medicine", CountryCode = "+1", PhoneNumber = "1234567890", Bio = "Experienced general practitioner.", YearsOfExperience = 10, Address = "123 Health St, Wellness City" }
        };
        private static int _nextDoctorId = 2;

        // Simulated data store for availability
        private static List<ExistingAvailabilitySlotViewModel> _doctorAvailability = new List<ExistingAvailabilitySlotViewModel>
        {
            new ExistingAvailabilitySlotViewModel { Id = 1, Date = DateTime.Today.AddDays(1), StartTime = new TimeSpan(9,0,0), EndTime = new TimeSpan(10,0,0), IsBooked = false, PatientNameIfBooked = null },
            new ExistingAvailabilitySlotViewModel { Id = 2, Date = DateTime.Today.AddDays(1), StartTime = new TimeSpan(10,0,0), EndTime = new TimeSpan(11,0,0), IsBooked = true, PatientNameIfBooked="Test Patient A" },
            new ExistingAvailabilitySlotViewModel { Id = 3, Date = DateTime.Today.AddDays(2), StartTime = new TimeSpan(14,0,0), EndTime = new TimeSpan(15,0,0), IsBooked = false, PatientNameIfBooked = null }
        };
        private static int _nextAvailabilityId = 4;

        // Simulated data store for all appointments
        private static List<AppointmentSummaryViewModel> _allAppointments = new List<AppointmentSummaryViewModel>
        {
            new AppointmentSummaryViewModel { Id = 1, PatientName = "Alice Wonderland", AppointmentDateTime = DateTime.Today.AddHours(10).AddMinutes(30), Status = "Confirmed" },
            new AppointmentSummaryViewModel { Id = 2, PatientName = "Bob The Builder", AppointmentDateTime = DateTime.Today.AddHours(14), Status = "Scheduled" },
            new AppointmentSummaryViewModel { Id = 3, PatientName = "Charlie Chaplin", AppointmentDateTime = DateTime.Today.AddDays(1).AddHours(9), Status = "Scheduled" },
            new AppointmentSummaryViewModel { Id = 4, PatientName = "Diana Prince", AppointmentDateTime = DateTime.Today.AddDays(2).AddHours(11), Status = "Confirmed" }
        };
        private static int _nextAppointmentId = 5;

        // === DOCTOR REGISTRATION ===
        [HttpGet]
        public IActionResult DoctorRegister()
        {
            return View("~/Views/Home/DoctorRegister.cshtml", new DoctorRegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoctorRegister(DoctorRegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var newDoctor = new DoctorProfileViewModel
                {
                    Id = _nextDoctorId++,
                    Name = model.Name,
                    Email = model.Email,
                    Specialization = model.Specialization,
                    CountryCode = model.CountryCode,
                    PhoneNumber = model.PhoneNumber,
                    Bio = null,
                    YearsOfExperience = null,
                    Address = null // Initialize Address
                };
                _doctorProfiles.Add(newDoctor);
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
            return View("~/Views/Home/DoctorLogin.cshtml", new DoctorLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoctorLogin(DoctorLoginViewModel model)
        {
            if (model == null)
            {
                ModelState.AddModelError(string.Empty, "Login data is missing.");
                return View("~/Views/Home/DoctorLogin.cshtml", new DoctorLoginViewModel());
            }
            if (ModelState.IsValid)
            {
                var doctorProfile = _doctorProfiles.FirstOrDefault(d => d.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
                if (doctorProfile != null && model.Password.Equals("Password123!"))
                {
                    HttpContext.Session.SetString("DoctorLoggedIn", "true");
                    HttpContext.Session.SetInt32("DoctorId", doctorProfile.Id);
                    HttpContext.Session.SetString("DoctorName", doctorProfile.Name);
                    HttpContext.Session.SetString("UserRole", "Doctor");
                    TempData["SuccessMessage"] = "Login successful!";
                    return RedirectToAction("Dashboard");
                }
                ModelState.AddModelError(string.Empty, "Invalid login attempt (simulated).");
            }
            return View("~/Views/Home/DoctorLogin.cshtml", model);
        }

        // === DOCTOR DASHBOARD & ACTIONS ===
        private bool IsDoctorLoggedIn()
        {
            return HttpContext.Session.GetString("DoctorLoggedIn") == "true" &&
                   HttpContext.Session.GetString("UserRole") == "Doctor";
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "You need to log in as a doctor to access the dashboard.";
                return RedirectToAction("DoctorLogin");
            }
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            var doctorProfile = _doctorProfiles.FirstOrDefault(d => d.Id == doctorId);
            var doctorName = doctorProfile?.Name ?? HttpContext.Session.GetString("DoctorName") ?? "Doctor";
            var viewModel = new DoctorDashboardViewModel { DoctorDisplayName = $"Dr. {doctorName}" };
            viewModel.TodaysAppointments = _allAppointments
                .Where(a => a.AppointmentDateTime.Date == DateTime.Today)
                .Select(a => new AppointmentSummaryViewModel { Id = a.Id, PatientName = a.PatientName, AppointmentDateTime = a.AppointmentDateTime, Status = a.Status }).ToList();
            viewModel.Notifications.Add("Reminder: Annual health checkup for patient ID P00123 is due next week.");
            viewModel.Notifications.Add("New message from Admin: Please update your emergency contact information.");
            if (TempData["SuccessMessage"] != null) ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View("~/Views/Home/DoctorDashboard.cshtml", viewModel);
        }

        // === DOCTOR PROFILE ACTIONS (Updated with refined ViewBag messages) ===
        [HttpGet]
        public IActionResult Profile()
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

            var doctorProfile = _doctorProfiles.FirstOrDefault(p => p.Id == doctorId.Value);
            if (doctorProfile == null)
            {
                TempData["ErrorMessage"] = "Your profile could not be found. Please contact support.";
                HttpContext.Session.Clear();
                return RedirectToAction("DoctorLogin");
            }

            // Messages from POST redirect or other sources
            if (TempData["ProfileSuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["ProfileSuccessMessage"];
            }
            if (TempData["ProfileErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ProfileErrorMessage"];
            }

            return View("~/Views/Home/DoctorProfile.cshtml", doctorProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(DoctorProfileViewModel model)
        {
            if (!IsDoctorLoggedIn())
            {
                return RedirectToAction("DoctorLogin");
            }

            var doctorIdFromSession = HttpContext.Session.GetInt32("DoctorId");
            if (doctorIdFromSession == null || model.Id != doctorIdFromSession.Value)
            {
                TempData["ProfileErrorMessage"] = "Unauthorized profile update attempt or session issue.";
                return RedirectToAction("Profile"); // Redirect back to profile GET to show message
            }

            var originalProfile = _doctorProfiles.FirstOrDefault(p => p.Id == model.Id);
            if (originalProfile != null)
            {
                model.Email = originalProfile.Email; // Preserve original email, as it's readonly in the form
            }
            else
            {
                TempData["ProfileErrorMessage"] = "Profile not found for update.";
                return RedirectToAction("Profile");
            }

            if (ModelState.IsValid)
            {
                var doctorToUpdate = originalProfile; // We already have it

                doctorToUpdate.Name = model.Name;
                doctorToUpdate.Specialization = model.Specialization;
                doctorToUpdate.CountryCode = model.CountryCode;
                doctorToUpdate.PhoneNumber = model.PhoneNumber;
                doctorToUpdate.Bio = model.Bio;
                doctorToUpdate.YearsOfExperience = model.YearsOfExperience;
                doctorToUpdate.Address = model.Address;

                HttpContext.Session.SetString("DoctorName", model.Name); // Update session if name changed
                TempData["ProfileSuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction("Profile");
            }

            // If ModelState is invalid, return to the view with the model to display errors
            ViewBag.IsEditMode = true; // JavaScript can use this to re-enable the form
            TempData["ProfileErrorMessage"] = "Please correct the validation errors noted below.";
            // We set TempData here so it's picked up by the GET action after redirect,
            // OR directly use ViewBag if not redirecting but re-rendering the view.
            // Since we are re-rendering, ViewBag is fine.
            ViewBag.ErrorMessage = "Please correct the validation errors noted below.";
            foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
            {
                ViewBag.ErrorMessage += "<br />" + error.ErrorMessage;
            }
            return View("~/Views/Home/DoctorProfile.cshtml", model);
        }


        // === AVAILABILITY ACTIONS ===
        [HttpGet]
        public IActionResult Availability()
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to manage availability.";
                return RedirectToAction("DoctorLogin");
            }
            var doctorId = HttpContext.Session.GetInt32("DoctorId");

            var viewModel = new DoctorManageAvailabilityViewModel
            {
                ExistingSlots = _doctorAvailability
                                   .OrderBy(s => s.Date)
                                   .ThenBy(s => s.StartTime)
                                   .ToList(),
                NewSlot = new AvailabilitySlotInputViewModel { Date = DateTime.Today.AddDays(1) }
            };

            if (TempData["AvailabilitySuccessMessage"] != null) ViewBag.SuccessMessage = TempData["AvailabilitySuccessMessage"];
            if (TempData["AvailabilityErrorMessage"] != null) ViewBag.ErrorMessage = TempData["AvailabilityErrorMessage"];

            return View("~/Views/Home/DoctorManageAvailability.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddAvailability(AvailabilitySlotInputViewModel newSlotInput)
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "Please log in to add availability.";
                return RedirectToAction("DoctorLogin");
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

                if (ModelState.IsValid)
                {
                    _doctorAvailability.Add(new ExistingAvailabilitySlotViewModel
                    {
                        Id = _nextAvailabilityId++,
                        Date = newSlotInput.Date,
                        StartTime = newSlotInput.StartTime,
                        EndTime = newSlotInput.EndTime,
                        IsBooked = false,
                        PatientNameIfBooked = null
                    });
                    TempData["AvailabilitySuccessMessage"] = "Availability slot added successfully!";
                    return RedirectToAction("Availability");
                }
            }

            TempData["AvailabilityErrorMessage"] = "Failed to add availability slot. Please check the details.";
            var fullViewModel = new DoctorManageAvailabilityViewModel
            {
                ExistingSlots = _doctorAvailability
                                   .OrderBy(s => s.Date)
                                   .ThenBy(s => s.StartTime)
                                   .ToList(),
                NewSlot = newSlotInput
            };
            return View("~/Views/Home/DoctorManageAvailability.cshtml", fullViewModel);
        }

        // === LOGOUT ===
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["GlobalSuccessMessage"] = "You have been successfully logged out.";
            return RedirectToAction("Index", "Home");
        }

        // Inside Patient_Appointment_Management_System/Controllers/DoctorController.cs

        // ... (your existing DoctorLogin, Dashboard, Profile, etc. methods) ...

        [HttpGet]
        public IActionResult DoctorForgotPassword()
        {
            // If there's a message from a previous attempt (e.g. validation error from POST)
            // it will be handled by the view.
            return View("~/Views/Home/DoctorForgotPassword.cshtml", new DoctorForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoctorForgotPassword(DoctorForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TODO:
                // 1. Check if the model.Email exists in your doctor user database (e.g., _doctorProfiles list for now).
                //    var doctorExists = _doctorProfiles.Any(d => d.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
                // 2. If it exists:
                //    a. Generate a unique, secure password reset token.
                //    b. Store this token in the database (or simulated store), associated with the doctor user, along with an expiry timestamp.
                //    c. Construct a password reset link (e.g., https://yourdomain.com/Doctor/ResetPassword?token=yourtoken).
                //    d. Send an email to model.Email containing this link.
                // 3. If it doesn't exist, you should generally show the same message as if it did to prevent email enumeration.

                // For demonstration purposes, we'll set a TempData message.
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";

                return RedirectToAction("DoctorLogin"); // Redirect to the doctor login page
            }

            // If ModelState is invalid, return the view with validation errors
            // The model (with its errors) will be passed back to the view.
            return View("~/Views/Home/DoctorForgotPassword.cshtml", model);
        }

        // You will also need actions for DoctorResetPassword (GET to show form, POST to update password) later
        // For example:
        // [HttpGet]
        // public IActionResult DoctorResetPassword(string token)
        // {
        //     // 1. Validate the token (exists, not expired, matches a user)
        //     // 2. If valid, show a view with password and confirm password fields.
        //     //    Pass the token to the view (e.g., in a hidden field).
        //     // If invalid, show an error message or redirect.
        //     return View("~/Views/Home/DoctorResetPassword.cshtml", new DoctorResetPasswordViewModel { Token = token });
        // }

        // [HttpPost]
        // [ValidateAntiForgeryToken]
        // public IActionResult DoctorResetPassword(DoctorResetPasswordViewModel model)
        // {
        //     // 1. Validate ModelState.
        //     // 2. Re-validate the model.Token.
        //     // 3. If valid, update the doctor's password in the database (or _doctorProfiles).
        //     // 4. Invalidate the token.
        //     // 5. Redirect to login with a success message.
        //     // If invalid, return the view with errors.
        // }


        // === LOGOUT === (This should be the last method or near the end)
        // ... (your existing Logout method) ...
    }
}