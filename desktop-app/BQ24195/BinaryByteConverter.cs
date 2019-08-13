using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace BQ24195
{
    public class BinaryByteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "????????";
            if (value.ToString() == "") return "";
            var i = (int) System.Convert.ChangeType(value, typeof(int));
            var bd = new StringBuilder();
            for (int b = 1 << 7; b >= 1; b = b >> 1)
            {
                bd.Append((i & b) != 0 ? "1" : "0");
            }

            return bd.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int r = 0;
            int cap = 0;
            foreach (var chr in value.ToString())
            {
                if (chr == '1')
                {
                    r = (r << 1) + 1;
                }
                else if (chr == '0')
                {
                    r = (r << 1);
                }

                cap++;
                if (cap == 8) break;
            }

            return System.Convert.ChangeType(r, targetType);
        }
    }
}