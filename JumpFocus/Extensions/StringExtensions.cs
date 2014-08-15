using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace JumpFocus.Extensions
{
    public static class StringExtensions
    {
        //Because twitter API doesn't use a standard datetime format
        public static DateTime ParseTwitterDateTime(this string input)
        {
            const string format = "ddd MMM dd HH:mm:ss zzzz yyyy";

            return DateTime.ParseExact(input, format, CultureInfo.InvariantCulture);
        }

        static public string ToBase64(this string input)
        {
            byte[] bytes = ASCIIEncoding.UTF8.GetBytes(input);

            return Convert.ToBase64String(bytes);
        }
    }
}
