using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FinalProject
{
    public class CustomValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty(value.ToString()))
                return new ValidationResult(false, "No number was entered!");
            if (value.ToString().Contains(' '))
                return new ValidationResult(false, "No spaces allowed!");

            return ValidationResult.ValidResult;
        }
    }
}
