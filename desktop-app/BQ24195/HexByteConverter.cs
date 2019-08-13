using System;
using System.Globalization;
using System.Windows.Data;

namespace BQ24195
{
    public class HexByteConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "??";
            if (value.ToString() == "") return "";
            var i = (int)System.Convert.ChangeType(value, typeof(int));
            return i.ToString("X2");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return int.Parse(value.ToString() ?? "", NumberStyles.HexNumber);
        }
    }
}