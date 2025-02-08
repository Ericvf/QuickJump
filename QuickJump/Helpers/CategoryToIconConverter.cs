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
                        return new BitmapImage(new Uri("pack://application:,,,/QuickJump;component/Resources/microsoft-azure.png"));
                    case Categories.Solution:
                        return new BitmapImage(new Uri("pack://application:,,,/QuickJump;component/Resources/visual-studio.png"));
                    case Categories.AzureDevOps:
                        return new BitmapImage(new Uri("pack://application:,,,/QuickJump;component/Resources/azure-devops.png"));
                    case Categories.ProcessWindow:
                        return new BitmapImage(new Uri("pack://application:,,,/QuickJump;component/Resources/process-icon.png"));
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
