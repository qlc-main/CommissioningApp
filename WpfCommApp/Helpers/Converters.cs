using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace WpfCommApp
{
    public class DefaultConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }

    public class StringArrayToStringConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] as string[] == null)
                return string.Empty;

            return (values[0] as string[])[(int)values[1] - 1];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Disable : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as string) == "NC";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ColumnCollapse : IValueConverter
    {

        // Collapses entire column for Forced Reason if all of forced values for this phase are 
        // false
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "Visible";

            foreach (Channel c in (value as ObservableCollection<Channel>))
            {
                if (c.Forced[(int)parameter] == true && (c.Phase1 == true || c.Phase2 == true))
                    return "Visible";
            }

            return "Collapsed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
