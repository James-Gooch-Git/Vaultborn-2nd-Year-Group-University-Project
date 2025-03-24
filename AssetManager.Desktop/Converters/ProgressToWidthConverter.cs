using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssetManager.Desktop.Converters
{
    public class ProgressToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double progress = (double)value;
            FrameworkElement element = parameter as FrameworkElement;

            if (element != null)
            {
                return progress * element.ActualWidth;
            }

            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}