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
        public int kMax { get; set; }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty(value.ToString()))
                return new ValidationResult(false, "No number was entered!");
            if (value.ToString().Contains(' '))
                return new ValidationResult(false, string.Format("No spaces allowed! {0}",kMax));
            try
            {
                int num = Convert.ToInt32(value);
                if (num == 0)
                    return new ValidationResult(false, string.Format("Number must be in range of (0,{0})", kMax));
            }
            catch (FormatException fe)
            {
                return new ValidationResult(false, fe.Message);
            }
            //if ((int)value == 0 || (int)value > kMax)
            //    return new ValidationResult(false, string.Format("Number must be in range of (0,{0})", kMax));
            return ValidationResult.ValidResult;
        }
    }
}
