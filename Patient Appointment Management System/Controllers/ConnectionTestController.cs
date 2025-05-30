using Microsoft.AspNetCore.Mvc;
using Patient_Appointment_Management_System.Data; // Ensure your namespace is correct

namespace Patient_Appointment_Management_System.Controllers
{
    public class ConnectionTestController : Controller
    {
        private readonly PatientAppointmentDbContext _context;

        public ConnectionTestController(PatientAppointmentDbContext context)
        {
            _context = context;
        }

        public IActionResult TestConnection()
        {
            try
            {
                // Attempt to access the database
                var patientCount = _context.Patients.Count();
                ViewBag.Message = "Successfully connected: " + patientCount + " patients found.";
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error occurred: " + ex.Message;
            }

            return View();
        }
    }
}