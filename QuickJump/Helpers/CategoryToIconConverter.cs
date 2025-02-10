using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace QuickJump.Helpers
{
    public class CategoryToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Categories category)
            {
                switch (category)
                {
                    case Categories.Azure:
                        return new BitmapImage(new Uri("pack://application:,,,/QuickJump;component/Resources/azureportal.png"));
                    case Categories.Solution:
                        return new BitmapImage(new Uri("pack://application:,,,/QuickJump;component/Resources/visualstudio.png"));
                    case Categories.AzureDevOps:
                        return new BitmapImage(new Uri("pack://application:,,,/QuickJump;component/Resources/azuredevops.png"));
                    case Categories.ProcessWindow:
                        return new BitmapImage(new Uri("pack://application:,,,/QuickJump;component/Resources/process.png"));
                    default:
                        return null;
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
