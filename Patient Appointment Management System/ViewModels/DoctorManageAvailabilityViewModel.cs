// File: Patient_Appointment_Management_System/ViewModels/DoctorManageAvailabilityViewModel.cs
using System;
using System.Collections.Generic;

namespace Patient_Appointment_Management_System.ViewModels
{
    public class DoctorManageAvailabilityViewModel
    {
        public List<ExistingAvailabilitySlotViewModel> ExistingSlots { get; set; }
        public AvailabilitySlotInputViewModel NewSlot { get; set; } // Property for the form

        public DoctorManageAvailabilityViewModel()
        {
            ExistingSlots = new List<ExistingAvailabilitySlotViewModel>();
            NewSlot = new AvailabilitySlotInputViewModel(); // Initialize the form model
        }
    }
}