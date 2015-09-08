using System;
using System.Text;

namespace Sassy.Extensions
{
    public static class StringExtensions
    {
        private const char Dash = '-';

        public static string AsCamelCased(this string str)
        {
            var components = str.Split(Dash);

            if (components.Length <= 1)
                return str;

            var camelCasedBuilder = new StringBuilder();
            for (var i = 0; i < components.Length; i++)
            {
                camelCasedBuilder.Append(i == 0
                    ? components[i] 
                    : components[i].WithCapitalizedFirstLetter());
            }

            return camelCasedBuilder.ToString();
        }

        public static string WithCapitalizedFirstLetter(this string str)
        {
            if (str.Length == 0) return str;

            var firstLetter = str.Substring(0, 1).ToUpper();

            var arra = str.ToCharArray();
            arra[0] = Convert.ToChar(firstLetter);
            return arra.ToString();
        }
    }
}