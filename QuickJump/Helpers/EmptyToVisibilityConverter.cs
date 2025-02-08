using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuickJump.Helpers
{

    public class EmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return Visibility.Visible; // Show placeholder when empty
            return Visibility.Collapsed;  // Hide placeholder when text is entered
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
