using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Patient_Appointment_Management_System.Data;
using Patient_Appointment_Management_System.Models;
using Patient_Appointment_Management_System.Utils;
using Patient_Appointment_Management_System.ViewModels;

using Microsoft.AspNetCore.Mvc.Rendering;


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

        private bool IsPatientLoggedIn() =>
            HttpContext.Session.GetString("PatientLoggedIn") == "true" &&
            HttpContext.Session.GetString("UserRole") == "Patient";

        private int? GetCurrentPatientId() => HttpContext.Session.GetInt32("PatientId");

        // --- Registration ---
        [HttpGet]
        public IActionResult PatientRegister() =>
            View("~/Views/Patient/PatientRegister.cshtml", new PatientRegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientRegister(PatientRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View("~/Views/Patient/PatientRegister.cshtml", model);
            if (await _context.Patients.AnyAsync(p => p.Email == model.Email))
            {
                ModelState.AddModelError("Email", "This email address is already registered.");
                return View("~/Views/Patient/PatientRegister.cshtml", model);
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
            TempData["RegisterSuccessMessage"] = "Registration successful! Please log in.";
            return RedirectToAction("PatientLogin");
        }

        // --- Login ---
        [HttpGet]
        public IActionResult PatientLogin()
        {
            ViewBag.SuccessMessage = TempData["RegisterSuccessMessage"] ?? TempData["GlobalSuccessMessage"];
            ViewBag.ErrorMessage = TempData["GlobalErrorMessage"] ?? TempData["ErrorMessage"];
            ViewBag.InfoMessage = TempData["ForgotPasswordMessage"];
            return View("~/Views/Account/PatientLogin.cshtml", new PatientLoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientLogin(PatientLoginViewModel model)
        {
            if (!ModelState.IsValid) return View("~/Views/Account/PatientLogin.cshtml", model);
            var patientUser = await _context.Patients.FirstOrDefaultAsync(p => p.Email == model.Email);
            if (patientUser != null && PasswordHelper.VerifyPassword(model.Password, patientUser.PasswordHash))
            {
                HttpContext.Session.SetString("PatientLoggedIn", "true");
                HttpContext.Session.SetInt32("PatientId", patientUser.PatientId);
                HttpContext.Session.SetString("PatientName", patientUser.Name);
                HttpContext.Session.SetString("UserRole", "Patient");
                TempData["GlobalSuccessMessage"] = "Login successful!";
                return RedirectToAction("PatientDashboard");
            }
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View("~/Views/Account/PatientLogin.cshtml", model);
        }

        // --- Dashboard ---
        [HttpGet]
        public async Task<IActionResult> PatientDashboard()
        {
            if (!IsPatientLoggedIn()) return RedirectToLogin("You need to log in as a patient to access the dashboard.");
            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToLogin("Session error. Please log in again.");
            var patient = await _context.Patients.FindAsync(patientId.Value);
            if (patient == null) return RedirectToLogin("Your account could not be found. Please log in again.", true);

            DateTime now = DateTime.Now;
            var upcomingAppointments = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId && a.AppointmentDateTime >= now && a.Status != "Cancelled" && a.Status != "Completed")
                .OrderBy(a => a.AppointmentDateTime)
                .Select(a => new AppointmentDetailViewModel
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDateTime = a.AppointmentDateTime,
                    DoctorName = a.Doctor.Name,
                    DoctorSpecialization = a.Doctor.Specialization,
                    Status = a.Status,
                    Issue = a.Issue
                }).ToListAsync();

            var appointmentHistory = await _context.Appointments
                .Include(a => a.Doctor)
                .Where(a => a.PatientId == patientId && (a.AppointmentDateTime < now || a.Status == "Completed" || a.Status == "Cancelled"))
                .OrderByDescending(a => a.AppointmentDateTime)
                .Select(a => new AppointmentDetailViewModel
                {
                    AppointmentId = a.AppointmentId,
                    AppointmentDateTime = a.AppointmentDateTime,
                    DoctorName = a.Doctor.Name,
                    DoctorSpecialization = a.Doctor.Specialization,
                    Status = a.Status,
                    Issue = a.Issue
                }).ToListAsync();

            var notifications = await _context.Notifications
                .Where(n => n.PatientId == patientId)
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
                }).ToListAsync();

            var unreadCount = await _context.Notifications.CountAsync(n => n.PatientId == patientId && !n.IsRead);

            var viewModel = new PatientDashboardViewModel
            {
                PatientName = patient.Name,
                UpcomingAppointments = upcomingAppointments,
                AppointmentHistory = appointmentHistory,
                Notifications = notifications,
                UnreadNotificationCount = unreadCount
            };

            ViewBag.SuccessMessage = TempData["GlobalSuccessMessage"] ?? TempData["SuccessMessage"];
            ViewBag.ErrorMessage = TempData["BookingErrorMessage"] ?? TempData["ErrorMessage"];
            return View("~/Views/Patient/PatientDashboard.cshtml", viewModel);
        }

        // --- Profile ---
        private async Task<PatientProfileViewModel?> GetPatientProfileViewModelAsync(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            return patient == null ? null : new PatientProfileViewModel
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
            if (!IsPatientLoggedIn()) return RedirectToLogin("Please log in to view your profile.");
            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToLogin("Session error. Please log in again.");
            var vm = await GetPatientProfileViewModelAsync(patientId.Value);
            if (vm == null) return RedirectToLogin("Your profile could not be found. Please log in again.", true);
            ViewData["ChangePasswordViewModel"] = new ChangePasswordViewModel();
            ViewBag.SuccessMessage = TempData["ProfileUpdateMessage"];
            ViewBag.ErrorMessage = TempData["ProfileUpdateError"];
            return View("~/Views/Patient/PatientProfile.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientProfile(PatientProfileViewModel model)
        {
            if (!IsPatientLoggedIn()) return RedirectToLogin("Please log in to update your profile.");
            var patientId = GetCurrentPatientId();
            if (patientId == null || model.Id != patientId.Value) return RedirectToLogin("Unauthorized profile update attempt or session mismatch.", true);

            ModelState.Remove("Dob");
            ModelState.Remove("Email");
            if (ModelState.IsValid)
            {
                var patient = await _context.Patients.FindAsync(model.Id);
                if (patient != null)
                {
                    patient.Name = model.Name;
                    patient.Phone = model.Phone;
                    patient.Address = model.Address;
                    _context.Patients.Update(patient);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("PatientName", patient.Name);
                    TempData["ProfileUpdateMessage"] = "Profile details updated successfully!";
                }
                else
                {
                    TempData["ProfileUpdateError"] = "Error: Profile not found for update.";
                }
                return RedirectToAction("PatientProfile");
            }
            var original = await GetPatientProfileViewModelAsync(model.Id);
            if (original != null)
            {
                model.Dob = original.Dob;
                model.Email = original.Email;
            }
            TempData["ProfileUpdateError"] = "Failed to update profile. Please check the errors.";
            ViewData["ChangePasswordViewModel"] = new ChangePasswordViewModel();
            return View("~/Views/Patient/PatientProfile.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel passwordModel)
        {
            if (!IsPatientLoggedIn()) return RedirectToLogin("Please log in to change your password.");
            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToLogin("Session error. Please log in again.");
            if (!ModelState.IsValid)
            {
                TempData["PasswordChangeError"] = "Failed to change password. Please check the errors below.";
                ViewData["ChangePasswordViewModel"] = passwordModel;
                var profileVm = await GetPatientProfileViewModelAsync(patientId.Value);
                return View("~/Views/Patient/PatientProfile.cshtml", profileVm);
            }
            var patient = await _context.Patients.FindAsync(patientId.Value);
            if (patient == null)
            {
                TempData["PasswordChangeError"] = "Patient account not found.";
                return RedirectToAction("PatientProfile");
            }
            if (!PasswordHelper.VerifyPassword(passwordModel.CurrentPassword, patient.PasswordHash))
            {
                TempData["PasswordChangeError"] = "Incorrect current password.";
                ViewData["ChangePasswordViewModel"] = passwordModel;
                var profileVm = await GetPatientProfileViewModelAsync(patientId.Value);
                return View("~/Views/Patient/PatientProfile.cshtml", profileVm);
            }
            patient.PasswordHash = PasswordHelper.HashPassword(passwordModel.NewPassword);
            await _context.SaveChangesAsync();
            TempData["PasswordChangeMessage"] = "Password changed successfully!";
            return RedirectToAction("PatientProfile");
        }

        // --- Logout ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PatientLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // --- Book Appointment ---
        [HttpGet]
        public async Task<IActionResult> BookAppointment()
        {
            if (!IsPatientLoggedIn()) return RedirectToLogin("Please log in to book an appointment.");
            var doctors = await _context.Doctors.OrderBy(d => d.Name)
                .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" }).ToListAsync();
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
            if (!IsPatientLoggedIn() || doctorId <= 0 || appointmentDate < DateTime.Today)
                return Json(new { error = "Invalid request", slots = new List<SelectListItem>() });
            var slots = await _context.AvailabilitySlots
                .Where(s => s.DoctorId == doctorId && s.Date.Date == appointmentDate.Date && !s.IsBooked &&
                            (s.Date.Date > DateTime.Today || (s.Date.Date == DateTime.Today && s.StartTime > DateTime.Now.TimeOfDay)))
                .OrderBy(s => s.StartTime)
                .ToListAsync();
            var formattedSlots = slots.Select(s => new SelectListItem
            {
                Value = s.AvailabilitySlotId.ToString(),
                Text = $"{DateTime.Today.Add(s.StartTime):hh:mm tt} - {DateTime.Today.Add(s.EndTime):hh:mm tt}"
            }).ToList();
            return Json(formattedSlots);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookAppointment(BookAppointmentViewModel model)
        {
            if (!IsPatientLoggedIn()) return RedirectToLogin("Please log in to book an appointment.");
            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToLogin("Session error. Please log in again.");
            if (model.SelectedAvailabilitySlotId <= 0 || model.DoctorId <= 0 || model.AppointmentDate < DateTime.Today)
            {
                ModelState.AddModelError("", "Please select all required fields.");
            }
            if (!ModelState.IsValid)
            {
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newAppointment = new Appointment
                {
                    PatientId = patientId.Value,
                    DoctorId = model.DoctorId,
                    AppointmentDateTime = chosenSlot.Date.Date.Add(chosenSlot.StartTime),
                    Status = "Scheduled",
                    Issue = model.Issue,
                    BookedAvailabilitySlotId = chosenSlot.AvailabilitySlotId
                };
                chosenSlot.IsBooked = true;
                _context.Appointments.Add(newAppointment);
                _context.AvailabilitySlots.Update(chosenSlot);
                await _context.SaveChangesAsync();
                await CreateNotificationAsync(null, chosenSlot.DoctorId,
                    $"New appointment booked by {HttpContext.Session.GetString("PatientName")} on {newAppointment.AppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt} - Issue: {model.Issue ?? "Not specified"}",
                    "Booking", "/Doctor/DoctorViewAppointment");
                await transaction.CommitAsync();
                TempData["SuccessMessage"] = $"Appointment with Dr. {chosenSlot.Doctor.Name} on {newAppointment.AppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt} has been successfully requested.";
                return RedirectToAction("PatientDashboard");
            }
            catch
            {
                await transaction.RollbackAsync();
                TempData["BookingErrorMessage"] = "The slot you are trying to book has been already cancelled by you.";
                await RepopulateBookAppointmentViewModelForPostErrorAsync(model);
                return View("~/Views/Patient/BookAppointment.cshtml", model);
            }
        }

        private async Task RepopulateBookAppointmentViewModelForPostErrorAsync(BookAppointmentViewModel model)
        {
            var doctors = await _context.Doctors.OrderBy(d => d.Name)
                .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" }).ToListAsync();
            model.DoctorsList = doctors.Select(d => new SelectListItem { Value = d.DoctorId.ToString(), Text = d.NameAndSpec }).ToList();
            model.AvailableTimeSlots = new List<SelectListItem>();
            if (model.DoctorId > 0 && model.AppointmentDate >= DateTime.Today)
            {
                var slots = await _context.AvailabilitySlots
                    .Where(s => s.DoctorId == model.DoctorId && s.Date.Date == model.AppointmentDate.Date && !s.IsBooked &&
                                (s.Date.Date > DateTime.Today || (s.Date.Date == DateTime.Today && s.StartTime > DateTime.Now.TimeOfDay)))
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();
                model.AvailableTimeSlots = slots.Select(s => new SelectListItem
                {
                    Value = s.AvailabilitySlotId.ToString(),
                    Text = $"{DateTime.Today.Add(s.StartTime):hh:mm tt} - {DateTime.Today.Add(s.EndTime):hh:mm tt}"
                }).ToList();
            }
        }

        // --- Forgot Password ---
        [HttpGet]
        public IActionResult PatientForgotPassword() =>
            View("~/Views/Account/PatientForgotPassword.cshtml", new PatientForgotPasswordViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PatientForgotPassword(PatientForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                TempData["ForgotPasswordMessage"] = "If an account with that email address exists, a password reset link has been sent. Please check your inbox (and spam folder).";
                return RedirectToAction("PatientLogin");
            }
            return View("~/Views/Account/PatientForgotPassword.cshtml", model);
        }

        // --- Cancel Appointment ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAppointment(int appointmentId)
        {
            if (!IsPatientLoggedIn()) return RedirectToLogin("Please log in to cancel an appointment.");
            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToLogin("Session error. Please log in again.");
            var appointment = await _context.Appointments
                .Include(a => a.BookedAvailabilitySlot)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.PatientId == patientId);
            if (appointment == null || appointment.Status == "Completed" || appointment.Status == "Cancelled" || appointment.AppointmentDateTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "This appointment cannot be cancelled.";
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
            await CreateNotificationAsync(null, appointment.DoctorId,
                $"Appointment cancelled by {HttpContext.Session.GetString("PatientName")} for {appointment.AppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt}",
                "Cancellation", "/Doctor/DoctorViewAppointment");
            TempData["SuccessMessage"] = $"Your appointment with Dr. {appointment.Doctor.Name} on {appointment.AppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt} has been cancelled successfully.";
            return RedirectToAction("PatientDashboard");
        }

        // --- Reschedule Appointment ---
        [HttpGet]
        public async Task<IActionResult> RescheduleAppointment(int appointmentId)
        {
            if (!IsPatientLoggedIn()) return RedirectToLogin("Please log in to reschedule an appointment.");
            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToLogin("Session error. Please log in again.");
            var appointment = await _context.Appointments.Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId && a.PatientId == patientId);
            if (appointment == null || appointment.Status == "Completed" || appointment.Status == "Cancelled" || appointment.AppointmentDateTime < DateTime.Now)
            {
                TempData["ErrorMessage"] = "This appointment cannot be rescheduled.";
                return RedirectToAction("PatientDashboard");
            }
            var doctors = await _context.Doctors.OrderBy(d => d.Name)
                .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" }).ToListAsync();
            var viewModel = new RescheduleAppointmentViewModel
            {
                AppointmentId = appointment.AppointmentId,
                CurrentDoctorId = appointment.DoctorId,
                CurrentDoctorName = $"Dr. {appointment.Doctor.Name} ({appointment.Doctor.Specialization})",
                CurrentAppointmentDateTime = appointment.AppointmentDateTime,
                Issue = appointment.Issue,
                DoctorId = appointment.DoctorId,
                AppointmentDate = DateTime.Today.AddDays(1),
                DoctorsList = doctors.Select(d => new SelectListItem
                {
                    Value = d.DoctorId.ToString(),
                    Text = d.NameAndSpec,
                    Selected = d.DoctorId == appointment.DoctorId
                }).ToList(),
                AvailableTimeSlots = new List<SelectListItem>()
            };
            return View("~/Views/Patient/RescheduleAppointment.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleAppointment(RescheduleAppointmentViewModel model)
        {
            if (!IsPatientLoggedIn()) return RedirectToLogin("Please log in to reschedule an appointment.");
            var patientId = GetCurrentPatientId();
            if (patientId == null) return RedirectToLogin("Session error. Please log in again.");
            if (model.SelectedAvailabilitySlotId <= 0)
            {
                ModelState.AddModelError(nameof(model.SelectedAvailabilitySlotId), "Please select an available time slot.");
            }
            if (!ModelState.IsValid)
            {
                await RepopulateRescheduleViewModelForPostErrorAsync(model);
                return View("~/Views/Patient/RescheduleAppointment.cshtml", model);
            }
            var appointment = await _context.Appointments
                .Include(a => a.BookedAvailabilitySlot)
                .Include(a => a.Doctor)
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId && a.PatientId == patientId);
            if (appointment == null || appointment.Status == "Completed" || appointment.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "This appointment cannot be rescheduled.";
                return RedirectToAction("PatientDashboard");
            }
            var newSlot = await _context.AvailabilitySlots.Include(s => s.Doctor)
                .FirstOrDefaultAsync(s => s.AvailabilitySlotId == model.SelectedAvailabilitySlotId &&
                                          s.DoctorId == model.DoctorId &&
                                          s.Date.Date == model.AppointmentDate.Date &&
                                          !s.IsBooked);
            if (newSlot == null || (newSlot.Date.Date == DateTime.Today && newSlot.StartTime <= DateTime.Now.TimeOfDay))
            {
                TempData["BookingErrorMessage"] = "The selected time slot is no longer available. Please choose another slot.";
                await RepopulateRescheduleViewModelForPostErrorAsync(model);
                return View("~/Views/Patient/RescheduleAppointment.cshtml", model);
            }
            DateTime newStart = newSlot.Date.Date.Add(newSlot.StartTime);
            DateTime newEnd = newSlot.Date.Date.Add(newSlot.EndTime);
            var patientAppointments = await _context.Appointments
                .Include(a => a.BookedAvailabilitySlot)
                .Where(a => a.PatientId == patientId && a.AppointmentId != model.AppointmentId && a.Status != "Cancelled" && a.Status != "Completed")
                .ToListAsync();
            bool hasConflict = patientAppointments.Any(a =>
            {
                DateTime existingEnd = a.BookedAvailabilitySlot != null
                    ? a.BookedAvailabilitySlot.Date.Date.Add(a.BookedAvailabilitySlot.EndTime)
                    : a.AppointmentDateTime.AddMinutes(30);
                return a.AppointmentDateTime < newEnd && existingEnd > newStart;
            });
            if (hasConflict)
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
            appointment.AppointmentDateTime = newStart;
            appointment.BookedAvailabilitySlotId = newSlot.AvailabilitySlotId;
            appointment.Issue = model.Issue;
            newSlot.IsBooked = true;
            _context.AvailabilitySlots.Update(newSlot);
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
            await CreateNotificationAsync(null, appointment.DoctorId,
                $"Appointment rescheduled by {HttpContext.Session.GetString("PatientName")} from {model.CurrentAppointmentDateTime:MMMM dd, yyyy 'at' hh:mm tt} to {newStart:MMMM dd, yyyy 'at' hh:mm tt}",
                "Reschedule", "/Doctor/DoctorViewAppointment");
            TempData["SuccessMessage"] = $"Your appointment has been rescheduled to {newStart:MMMM dd, yyyy 'at' hh:mm tt} with Dr. {newSlot.Doctor.Name}.";
            return RedirectToAction("PatientDashboard");
        }

        private async Task RepopulateRescheduleViewModelForPostErrorAsync(RescheduleAppointmentViewModel model)
        {
            var appointment = await _context.Appointments.Include(a => a.Doctor).AsNoTracking()
                .FirstOrDefaultAsync(a => a.AppointmentId == model.AppointmentId);
            if (appointment != null)
            {
                model.CurrentDoctorId = appointment.DoctorId;
                model.CurrentDoctorName = $"Dr. {appointment.Doctor.Name} ({appointment.Doctor.Specialization})";
                model.CurrentAppointmentDateTime = appointment.AppointmentDateTime;
            }
            var doctors = await _context.Doctors.OrderBy(d => d.Name)
                .Select(d => new { d.DoctorId, NameAndSpec = $"Dr. {d.Name} ({d.Specialization})" }).ToListAsync();
            model.DoctorsList = doctors.Select(d => new SelectListItem
            {
                Value = d.DoctorId.ToString(),
                Text = d.NameAndSpec
            }).ToList();
            model.AvailableTimeSlots = new List<SelectListItem>();
            if (model.DoctorId > 0 && model.AppointmentDate != default && model.AppointmentDate >= DateTime.Today)
            {
                var slots = await _context.AvailabilitySlots
                    .Where(s => s.DoctorId == model.DoctorId && s.Date.Date == model.AppointmentDate.Date && !s.IsBooked &&
                                (s.Date.Date > DateTime.Today || (s.Date.Date == DateTime.Today && s.StartTime > DateTime.Now.TimeOfDay)))
                    .OrderBy(s => s.StartTime)
                    .ToListAsync();
                model.AvailableTimeSlots = slots.Select(s => new SelectListItem
                {
                    Value = s.AvailabilitySlotId.ToString(),
                    Text = $"{DateTime.Today.Add(s.StartTime):hh:mm tt} - {DateTime.Today.Add(s.EndTime):hh:mm tt}"
                }).ToList();
            }
        }

        // --- Notification ---
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
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Notification not found." });
        }

        // --- Helper for login redirect with error ---
        private IActionResult RedirectToLogin(string error, bool clearSession = false)
        {
            if (clearSession) HttpContext.Session.Clear();
            TempData["ErrorMessage"] = error;
            return RedirectToAction("PatientLogin");
        }
    }
}
