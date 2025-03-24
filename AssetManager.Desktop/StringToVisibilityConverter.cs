using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AssetManager.Desktop;

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string visibility && visibility.Equals("Visible", StringComparison.OrdinalIgnoreCase))
        {
            return Visibility.Visible;
        }
        return Visibility.Collapsed;
    }
    
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return DependencyProperty.UnsetValue;
    }
}
