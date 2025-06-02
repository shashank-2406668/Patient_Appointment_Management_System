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
using System.Globalization; // Keep if used elsewhere

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
                    Phone = model.CountryCode + model.PhoneNumber
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
                    PatientName = a.Patient.Name,
                    AppointmentDateTime = a.AppointmentDateTime,
                    Status = a.Status,
                    Issue = a.Issue
                })
                .ToListAsync();

            var viewModel = new DoctorDashboardViewModel
            {
                DoctorDisplayName = $"Dr. {doctor.Name}",
                TodaysAppointments = todaysAppointments,
                Notifications = new List<string> { "Review your schedule for today." } // Example notification
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
            if (doctor == null) { HttpContext.Session.Clear(); return RedirectToAction("DoctorLogin"); }

            var vm = new DoctorProfileViewModel { Id = doctor.DoctorId, Name = doctor.Name, Email = doctor.Email, Specialization = doctor.Specialization };
            if (!string.IsNullOrEmpty(doctor.Phone))
            {
                if (doctor.Phone.StartsWith("+"))
                {
                    int firstDigitAfterPlus = -1;
                    for (int i = 1; i < doctor.Phone.Length; i++)
                    {
                        if (char.IsDigit(doctor.Phone[i]))
                        {
                            firstDigitAfterPlus = i;
                            break;
                        }
                    }

                    if (firstDigitAfterPlus != -1)
                    {
                        int codeEndIndex = firstDigitAfterPlus;
                        while (codeEndIndex < doctor.Phone.Length && codeEndIndex < firstDigitAfterPlus + 3 && char.IsDigit(doctor.Phone[codeEndIndex]))
                        {
                            codeEndIndex++;
                        }
                        vm.CountryCode = doctor.Phone.Substring(0, codeEndIndex);
                        vm.PhoneNumber = doctor.Phone.Substring(codeEndIndex);
                    }
                    else
                    {
                        vm.PhoneNumber = doctor.Phone;
                    }
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
                doctorToUpdate.Phone = model.CountryCode + model.PhoneNumber;

                try
                {
                    _context.Doctors.Update(doctorToUpdate);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("DoctorName", doctorToUpdate.Name);
                    TempData["ProfileSuccessMessage"] = "Profile updated successfully!";
                }
                catch (Exception ex) { _logger.LogError(ex, "Error updating profile for Doctor ID {DoctorId}", model.Id); TempData["ProfileErrorMessage"] = "An error occurred while updating your profile."; }
                return RedirectToAction("Profile");
            }
            TempData["ProfileErrorMessage"] = "Please correct validation errors.";
            return View("~/Views/Home/DoctorProfile.cshtml", model);
        }


        // === AVAILABILITY ACTIONS ===
        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin");

            var existingSlotsFromDb = await _context.AvailabilitySlots
                .Include(s => s.BookedByAppointment)
                    .ThenInclude(appt => appt.Patient)
                .Where(s => s.DoctorId == doctorId.Value && s.Date >= DateTime.Today)
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new ExistingAvailabilitySlotViewModel
                {
                    Id = s.AvailabilitySlotId,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsBooked = s.IsBooked,
                    PatientNameIfBooked = s.IsBooked && s.BookedByAppointment != null && s.BookedByAppointment.Patient != null ? s.BookedByAppointment.Patient.Name : null,
                    AppointmentIdIfBooked = s.BookedByAppointmentId
                })
                .ToListAsync();

            var viewModel = new DoctorManageAvailabilityViewModel
            {
                ExistingSlots = existingSlotsFromDb,
                NewSlot = new AvailabilitySlotInputViewModel { Date = DateTime.Today.AddDays(1) }
            };

            if (TempData["AvailabilitySuccessMessage"] != null) ViewBag.SuccessMessage = TempData["AvailabilitySuccessMessage"];
            if (TempData["AvailabilityErrorMessage"] != null) ViewBag.ErrorMessage = TempData["AvailabilityErrorMessage"];
            return View("~/Views/Home/DoctorManageAvailability.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(AvailabilitySlotInputViewModel newSlotInput)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("Availability");

            if (ModelState.IsValid)
            {
                if (newSlotInput.EndTime <= newSlotInput.StartTime)
                    ModelState.AddModelError("NewSlot.EndTime", "End time must be after start time.");
                if (newSlotInput.Date < DateTime.Today)
                    ModelState.AddModelError("NewSlot.Date", "Availability date cannot be in the past.");

                bool overlaps = await _context.AvailabilitySlots.AnyAsync(s =>
                    s.DoctorId == doctorId.Value && s.Date == newSlotInput.Date &&
                    newSlotInput.StartTime < s.EndTime && newSlotInput.EndTime > s.StartTime);
                if (overlaps)
                    ModelState.AddModelError("NewSlot.StartTime", "This time slot overlaps with an existing one for this date.");

                if (ModelState.IsValid)
                {
                    var availabilitySlot = new AvailabilitySlot
                    {
                        DoctorId = doctorId.Value,
                        Date = newSlotInput.Date,
                        StartTime = newSlotInput.StartTime,
                        EndTime = newSlotInput.EndTime,
                        IsBooked = false
                    };
                    _context.AvailabilitySlots.Add(availabilitySlot);
                    await _context.SaveChangesAsync();
                    TempData["AvailabilitySuccessMessage"] = "Availability slot added successfully!";
                    return RedirectToAction("Availability");
                }
            }
            TempData["AvailabilityErrorMessage"] = "Failed to add slot. Please check errors.";

            var existingSlotsFromDb = await _context.AvailabilitySlots
                .Include(s => s.BookedByAppointment).ThenInclude(appt => appt.Patient)
                .Where(s => s.DoctorId == doctorId.Value && s.Date >= DateTime.Today)
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new ExistingAvailabilitySlotViewModel { Id = s.AvailabilitySlotId, Date = s.Date, StartTime = s.StartTime, EndTime = s.EndTime, IsBooked = s.IsBooked, PatientNameIfBooked = s.IsBooked && s.BookedByAppointment != null && s.BookedByAppointment.Patient != null ? s.BookedByAppointment.Patient.Name : null, AppointmentIdIfBooked = s.BookedByAppointmentId })
                .ToListAsync();
            var fullViewModel = new DoctorManageAvailabilityViewModel { ExistingSlots = existingSlotsFromDb, NewSlot = newSlotInput };
            return View("~/Views/Home/DoctorManageAvailability.cshtml", fullViewModel);
        }

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
                return Json(new { success = false, message = "Slot not found or access denied." });

            if (slotToDelete.IsBooked)
                return Json(new { success = false, message = "Cannot delete a booked availability slot. Please ask the patient to cancel the appointment." });

            if (slotToDelete.Date < DateTime.Today || (slotToDelete.Date == DateTime.Today && slotToDelete.StartTime < DateTime.Now.TimeOfDay))
            {
                return Json(new { success = false, message = "Cannot delete past availability slots." });
            }

            _context.AvailabilitySlots.Remove(slotToDelete);
            await _context.SaveChangesAsync();
            // TempData won't be seen by the client with a Json result, but can be useful for logging/server-side state
            TempData["AvailabilitySuccessMessage"] = "Availability slot deleted successfully!";
            return Json(new { success = true, message = "Availability slot deleted successfully!" });
        }

        // === VIEW ALL APPOINTMENTS FOR DOCTOR ===
        [HttpGet]
        public async Task<IActionResult> DoctorViewAppointment() // Action name matches asp-action
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
                .Include(a => a.Patient) // Make sure Patient is included if PatientName comes from Patient model
                .Where(a => a.DoctorId == doctorId.Value)
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => new AppointmentSummaryViewModel
                {
                    Id = a.AppointmentId,
                    PatientName = a.Patient.Name, // Assumes Patient navigation property has Name
                    AppointmentDateTime = a.AppointmentDateTime,
                    Status = a.Status,
                    Issue = a.Issue
                })
                .ToListAsync();

            // Explicitly point to the view in Views/Home folder
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
                // For security, don't confirm if the email exists. Just say "if it exists, an email has been sent"
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
            return RedirectToAction("Index", "Home"); // Redirect to Home/Index or your main landing page
        }
    }
}