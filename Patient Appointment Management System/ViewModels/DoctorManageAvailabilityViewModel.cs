// File: Patient_Appointment_Management_System/ViewModels/DoctorManageAvailabilityViewModel.cs
using System.Collections.Generic;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorManageAvailabilityViewModel
    {
        public List<ExistingAvailabilitySlotViewModel> ExistingSlots { get; set; } = new List<ExistingAvailabilitySlotViewModel>();
        public AvailabilitySlotInputViewModel NewSlot { get; set; } = new AvailabilitySlotInputViewModel();
    }
}