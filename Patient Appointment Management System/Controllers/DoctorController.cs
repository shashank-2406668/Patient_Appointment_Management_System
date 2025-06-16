using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using Patient_Appointment_Management_System.ViewModels;
using Patient_Appointment_Management_System.Services;

namespace Patient_Appointment_Management_System.Controllers
{
    // This controller handles all doctor-related actions
    public class DoctorController : Controller
    {
        private readonly IDoctorService _doctorService;
        private readonly PatientAppointmentDbContext _context;
        private readonly ILogger<DoctorController> _logger;

        // Constructor to get required services
        public DoctorController(IDoctorService doctorService, PatientAppointmentDbContext context, ILogger<DoctorController> logger)
        {
            _doctorService = doctorService;
            _context = context;
            _logger = logger;
        }

        // Helper to check if doctor is logged in
        private bool IsDoctorLoggedIn()
        {
            return HttpContext.Session.GetString("DoctorLoggedIn") == "true" &&
                   HttpContext.Session.GetString("UserRole") == "Doctor";
        }

        // ========== DOCTOR LOGIN ==========
        [HttpGet]
        public IActionResult DoctorLogin()
        {
            // Show login page with any messages
            ViewBag.SuccessMessage = TempData["DoctorRegisterSuccessMessage"] ?? TempData["GlobalSuccessMessage"];
            ViewBag.InfoMessage = TempData["DoctorLogoutMessage"] ?? TempData["ForgotPasswordMessage"];
            ViewBag.ErrorMessage = TempData["ErrorMessage"];
            return View("~/Views/Account/DoctorLogin.cshtml", new DoctorLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorLogin(DoctorLoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View("~/Views/Account/DoctorLogin.cshtml", model);

            var doctor = await _doctorService.ValidateDoctorCredentialsAsync(model.Email, model.Password);
            if (doctor != null)
            {
                // Set session for doctor
                HttpContext.Session.SetString("DoctorLoggedIn", "true");
                HttpContext.Session.SetInt32("DoctorId", doctor.DoctorId);
                HttpContext.Session.SetString("DoctorName", doctor.Name);
                HttpContext.Session.SetString("UserRole", "Doctor");
                TempData["SuccessMessage"] = "Login successful!";
                return RedirectToAction("Dashboard");
            }
            ModelState.AddModelError("", "Invalid email or password.");
            return View("~/Views/Account/DoctorLogin.cshtml", model);
        }

        // ========== DOCTOR DASHBOARD ==========
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsDoctorLoggedIn())
            {
                TempData["ErrorMessage"] = "You need to log in as a doctor.";
                return RedirectToAction("DoctorLogin");
            }
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null)
            {
                TempData["ErrorMessage"] = "Session error. Please log in again.";
                return RedirectToAction("DoctorLogin");
            }

            var doctor = await _doctorService.GetDoctorByIdAsync(doctorId.Value);
            if (doctor == null)
            {
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Your account could not be found.";
                return RedirectToAction("DoctorLogin");
            }

            // Get today's appointments
            var todaysAppointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId && a.AppointmentDateTime.Date == DateTime.Today && (a.Status == "Scheduled" || a.Status == "Confirmed"))
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new AppointmentSummaryViewModel
                {
                    Id = a.AppointmentId,
                    AppointmentDateTime = a.AppointmentDateTime,
                    PatientName = a.Patient.Name,
                    Status = a.Status
                }).ToListAsync();

            // Get notifications
            var notifications = await _context.Notifications
                .Where(n => n.DoctorId == doctorId)
                .OrderByDescending(n => n.SentDate).Take(10)
                .Select(n => new NotificationViewModel
                {
                    NotificationId = n.NotificationId,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    SentDate = n.SentDate,
                    IsRead = n.IsRead,
                    Url = n.Url
                }).ToListAsync();

            var unreadCount = await _context.Notifications.CountAsync(n => n.DoctorId == doctorId && !n.IsRead);

            var viewModel = new DoctorDashboardViewModel
            {
                DoctorDisplayName = $"Dr. {doctor.Name}",
                TodaysAppointments = todaysAppointments,
                Notifications = notifications,
                UnreadNotificationCount = unreadCount
            };

            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View("~/Views/Doctor/DoctorDashboard.cshtml", viewModel);
        }

        // ========== DOCTOR PROFILE ==========
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin");

            var doctor = await _doctorService.GetDoctorByIdAsync(doctorId.Value);
            if (doctor == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("DoctorLogin");
            }

            // Split phone into country code and number
            var vm = new DoctorProfileViewModel
            {
                Id = doctor.DoctorId,
                Name = doctor.Name,
                Email = doctor.Email,
                Specialization = doctor.Specialization
            };
            if (!string.IsNullOrEmpty(doctor.Phone))
            {
                var codes = new[] { "+91", "+1", "+44", "+81", "+86" };
                vm.CountryCode = codes.FirstOrDefault(code => doctor.Phone.StartsWith(code));
                vm.PhoneNumber = vm.CountryCode != null ? doctor.Phone.Substring(vm.CountryCode.Length) : doctor.Phone;
            }
            ViewBag.SuccessMessage = TempData["ProfileSuccessMessage"];
            ViewBag.ErrorMessage = TempData["ProfileErrorMessage"];
            return View("~/Views/Doctor/DoctorProfile.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(DoctorProfileViewModel model)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null || model.Id != doctorId.Value) return RedirectToAction("Profile");

            ModelState.Remove("ChangePassword");
            if (ModelState.IsValid)
            {
                var doctor = await _doctorService.GetDoctorByIdAsync(model.Id);
                if (doctor == null) return RedirectToAction("Profile");
                doctor.Name = model.Name;
                doctor.Specialization = model.Specialization;
                doctor.Phone = (model.CountryCode ?? "") + (model.PhoneNumber ?? "");
                var success = await _doctorService.UpdateDoctorProfileAsync(doctor);
                TempData["ProfileSuccessMessage"] = success ? "Profile updated!" : "Error updating profile.";
                return RedirectToAction("Profile");
            }
            TempData["ProfileErrorMessage"] = "Please correct the errors.";
            return View("~/Views/Doctor/DoctorProfile.cshtml", model);
        }

        // ========== CHANGE PASSWORD ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(DoctorProfileViewModel model)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin");

            var pwd = model.ChangePassword;
            if (string.IsNullOrEmpty(pwd.CurrentPassword) || string.IsNullOrEmpty(pwd.NewPassword) || pwd.NewPassword != pwd.ConfirmPassword)
            {
                TempData["PasswordChangeError"] = "Fill all password fields correctly.";
                return RedirectToAction("Profile");
            }
            if (pwd.NewPassword.Length < 8)
            {
                TempData["PasswordChangeError"] = "New password must be at least 8 characters.";
                return RedirectToAction("Profile");
            }
            var doctor = await _context.Doctors.FindAsync(doctorId.Value);
            if (doctor == null || !PasswordHelper.VerifyPassword(pwd.CurrentPassword, doctor.PasswordHash))
            {
                TempData["PasswordChangeError"] = "Incorrect current password.";
                return RedirectToAction("Profile");
            }
            doctor.PasswordHash = PasswordHelper.HashPassword(pwd.NewPassword);
            await _context.SaveChangesAsync();
            TempData["PasswordChangeSuccess"] = "Password changed!";
            return RedirectToAction("Profile");
        }

        // ========== NOTIFICATIONS ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkNotificationAsRead(int notificationId)
        {
            if (!IsDoctorLoggedIn()) return Json(new { success = false });
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            var notification = await _context.Notifications.FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.DoctorId == doctorId);
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
            var notifications = await _context.Notifications.Where(n => n.DoctorId == doctorId && !n.IsRead).ToListAsync();
            notifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Helper to create a notification
        private async Task CreateNotificationAsync(int? patientId, int? doctorId, string message, string type, string? url = null)
        {
            var notification = new Notification
            {
                PatientId = patientId,
                DoctorId = doctorId,
                Message = message,
                NotificationType = type,
                SentDate = DateTime.Now,
                IsRead = false,
                Url = url
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // ========== AVAILABILITY ==========
        [HttpGet]
        public async Task<IActionResult> Availability()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin");

            var slots = await _context.AvailabilitySlots
                .Include(s => s.BookedByAppointment).ThenInclude(a => a.Patient)
                .Where(s => s.DoctorId == doctorId && s.Date >= DateTime.Today)
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
                }).ToListAsync();

            var viewModel = new DoctorManageAvailabilityViewModel
            {
                ExistingSlots = slots,
                NewSlot = new AvailabilitySlotInputViewModel { Date = DateTime.Today.AddDays(1) }
            };
            ViewBag.SuccessMessage = TempData["AvailabilitySuccessMessage"];
            ViewBag.ErrorMessage = TempData["AvailabilityErrorMessage"];
            return View("~/Views/Doctor/DoctorManageAvailability.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAvailability(DoctorManageAvailabilityViewModel model)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("Availability");

            var slot = model.NewSlot;
            if (ModelState.IsValid)
            {
                if (slot.EndTime <= slot.StartTime)
                    ModelState.AddModelError("NewSlot.EndTime", "End time must be after start time.");
                if (slot.Date.Date < DateTime.Today)
                    ModelState.AddModelError("NewSlot.Date", "Date cannot be in the past.");
                bool overlaps = await _context.AvailabilitySlots.AnyAsync(s =>
                    s.DoctorId == doctorId && s.Date.Date == slot.Date.Date &&
                    slot.StartTime < s.EndTime && slot.EndTime > s.StartTime);
                if (overlaps)
                    ModelState.AddModelError("NewSlot.StartTime", "This time slot overlaps with an existing one.");
                if (ModelState.IsValid)
                {
                    var availabilitySlot = new AvailabilitySlot
                    {
                        DoctorId = doctorId.Value,
                        Date = slot.Date.Date,
                        StartTime = slot.StartTime,
                        EndTime = slot.EndTime,
                        IsBooked = false
                    };
                    _context.AvailabilitySlots.Add(availabilitySlot);
                    await _context.SaveChangesAsync();
                    TempData["AvailabilitySuccessMessage"] = "Slot added!";
                    return RedirectToAction("Availability");
                }
            }
            TempData["AvailabilityErrorMessage"] = "Failed to add slot. Please check the errors.";
            model.ExistingSlots = await _context.AvailabilitySlots
                .Where(s => s.DoctorId == doctorId && s.Date >= DateTime.Today)
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new ExistingAvailabilitySlotViewModel
                {
                    Id = s.AvailabilitySlotId,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    IsBooked = s.IsBooked
                }).ToListAsync();
            return View("~/Views/Doctor/DoctorManageAvailability.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAvailability(int slotId)
        {
            if (!IsDoctorLoggedIn()) return Json(new { success = false, message = "Unauthorized." });
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return Json(new { success = false, message = "Session error." });

            var slot = await _context.AvailabilitySlots.FirstOrDefaultAsync(s => s.AvailabilitySlotId == slotId && s.DoctorId == doctorId);
            if (slot == null) return Json(new { success = false, message = "Slot not found." });
            if (slot.IsBooked) return Json(new { success = false, message = "Cannot delete a booked slot." });
            if (slot.Date < DateTime.Today || (slot.Date == DateTime.Today && slot.StartTime < DateTime.Now.TimeOfDay))
                return Json(new { success = false, message = "Cannot delete past or active slots." });

            _context.AvailabilitySlots.Remove(slot);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Slot deleted!" });
        }

        // ========== VIEW APPOINTMENTS ==========
        [HttpGet]
        public async Task<IActionResult> DoctorViewAppointment()
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin");

            var appointments = await _context.Appointments
                .Include(a => a.Patient)
                .Where(a => a.DoctorId == doctorId)
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => new AppointmentSummaryViewModel
                {
                    Id = a.AppointmentId,
                    PatientName = a.Patient.Name,
                    AppointmentDateTime = a.AppointmentDateTime,
                    Status = a.Status,
                    Issue = a.Issue
                }).ToListAsync();

            return View("~/Views/Doctor/DoctorViewAppointment.cshtml", appointments);
        }

        // ========== FORGOT PASSWORD ==========
        [HttpGet]
        public IActionResult DoctorForgotPassword()
        {
            return View("~/Views/Account/DoctorForgotPassword.cshtml", new DoctorForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoctorForgotPassword(DoctorForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                TempData["ForgotPasswordMessage"] = "If an account with that email exists, a reset link has been sent.";
                return RedirectToAction("DoctorLogin");
            }
            return View("~/Views/Account/DoctorForgotPassword.cshtml", model);
        }

        // ========== LOGOUT ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // ========== MARK APPOINTMENT AS COMPLETED ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsCompleted(int appointmentId)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin");

            var appointment = await _context.Appointments.Include(a => a.Patient)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctorId);
            if (appointment == null || appointment.AppointmentDateTime > DateTime.Now)
            {
                TempData["AvailabilityErrorMessage"] = "Cannot mark as completed.";
                return RedirectToAction("DoctorViewAppointment");
            }
            appointment.Status = "Completed";
            await CreateNotificationAsync(appointment.PatientId, null,
                $"Your appointment on {appointment.AppointmentDateTime:MMM dd, yyyy 'at' hh:mm tt} has been marked as completed.",
                "AppointmentCompleted", "/Patient/PatientDashboard");
            await _context.SaveChangesAsync();
            TempData["AvailabilitySuccessMessage"] = "Appointment marked as completed.";
            return RedirectToAction("DoctorViewAppointment");
        }

        // ========== CANCEL APPOINTMENT ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            if (!IsDoctorLoggedIn()) return RedirectToAction("DoctorLogin");
            var doctorId = HttpContext.Session.GetInt32("DoctorId");
            if (doctorId == null) return RedirectToAction("DoctorLogin");

            var appointment = await _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.BookedAvailabilitySlot)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.DoctorId == doctorId);

            if (appointment == null || appointment.AppointmentDateTime < DateTime.Now || appointment.Status == "Cancelled")
            {
                TempData["AvailabilityErrorMessage"] = "Cannot cancel this appointment.";
                return RedirectToAction("DoctorViewAppointment");
            }

            appointment.Status = "Cancelled";
            if (appointment.BookedAvailabilitySlot != null)
            {
                appointment.BookedAvailabilitySlot.IsBooked = false;
                appointment.BookedAvailabilitySlot.BookedByAppointmentId = null;
            }
            await CreateNotificationAsync(appointment.PatientId, null,
                $"Your appointment with Dr. {HttpContext.Session.GetString("DoctorName")} on {appointment.AppointmentDateTime:MMM dd, yyyy hh:mm tt} has been cancelled.",
                "AppointmentCancelled", "/Patient/ViewAppointments");
            await _context.SaveChangesAsync();
            TempData["AvailabilitySuccessMessage"] = "Appointment cancelled.";
            return RedirectToAction("DoctorViewAppointment");
        }
    }
}
