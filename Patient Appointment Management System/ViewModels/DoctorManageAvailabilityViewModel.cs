using System.Collections.Generic;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorManageAvailabilityViewModel
    {
        public AvailabilitySlotInputViewModel NewSlot { get; set; } = new AvailabilitySlotInputViewModel();
        public List<ExistingAvailabilitySlotViewModel> ExistingSlots { get; set; } = new List<ExistingAvailabilitySlotViewModel>();
    }
}