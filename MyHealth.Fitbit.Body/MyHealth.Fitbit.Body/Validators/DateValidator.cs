using System;
using System.Globalization;

namespace MyHealth.Fitbit.Body.Validators
{
    public class DateValidator : IDateValidator
    {
        public bool IsDateValid(string date)
        {
            bool isDateValid = false;
            string pattern = "yyyy-MM-dd";
            DateTime parsedDate;

            if (DateTime.TryParseExact(date, pattern, null, DateTimeStyles.None, out parsedDate))
            {
                isDateValid = true;
            }

            return isDateValid;
        }
    }
}
