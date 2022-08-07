using System;
using System.Globalization;

namespace admin.app.Extensions
{
    public static class StringExtensions
    {
        public static DateTime ParseDateWithCulture(this string date)
        {
            CultureInfo cultureinfo = new CultureInfo("en-GB");
            return DateTime.Parse(date, cultureinfo);
        }
    }
}
