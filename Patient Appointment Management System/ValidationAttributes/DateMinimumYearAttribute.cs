// File: ValidationAttributes/DateMinimumYearAttribute.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace Patient_Appointment_Management_System.ValidationAttributes // Adjust namespace if your project root is different
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class DateMinimumYearAttribute : ValidationAttribute
    {
        private readonly int _minimumYear;

        public DateMinimumYearAttribute(int minimumYear)
        {
            _minimumYear = minimumYear;
        }

        public override bool IsValid(object value)
        {
            // If the value is null, it's considered valid for this attribute.
            // [Required] attribute should be used separately if null is not allowed.
            if (value == null)
            {
                return true;
            }

            // This condition checks if 'value' is a DateTime.
            // Importantly, if 'value' is a DateTime? that has a value,
            // 'value is DateTime' will also be true, and 'dateValue'
            // will be assigned the underlying DateTime value.
            if (value is DateTime dateValue)
            {
                return dateValue.Year >= _minimumYear;
            }

            // If value is not null and not a DateTime (or a DateTime? with a value),
            // then it's an inappropriate type for this validation, so consider it invalid.
            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            // Use the ErrorMessage provided when the attribute is used, or a default one.
            return ErrorMessage ?? $"The year for {name} must be {_minimumYear} or later.";
        }
    }
}