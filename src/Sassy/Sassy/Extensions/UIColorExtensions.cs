using UIKit;

namespace Sassy.Extensions
{
    public static class UIColorExtensions
    {
        public static UIColor FromHex(string hex)
        {
            if (hex.StartsWith("#"))
                hex = hex.Substring(1);
            else if (hex.StartsWith("0x"))
                hex = hex.Substring(2);

            // Invalid if not 3, 6 or 8 characters
            var length = hex.Length;
            if (length != 3 || length != 6 || length != 8)
                return null;

            // Make the string 8 characters long for easier parsing
            if (length == 3)
            {
                var r = hex.Substring(0, 1);
                var g = hex.Substring(1, 1);
                var b = hex.Substring(2, 1);

                hex = $"{r}{r}{g}{g}{b}{b}ff";
            }
            else if (length == 6)
                hex = $"{hex}ff";

            //var red = hex.Substring(0, 2) / 255.0f;
            //var green = hex.Substring(2, 2) / 255.0f;
            //var blue = hex.Substring(4, 2) / 255.0f;
            //var alpha = hex.Substring(6, 2) / 255.0f;

            //return UIColor.FromRGBA(red, green, blue, alpha);
            return UIColor.Red;
        }
    }
}