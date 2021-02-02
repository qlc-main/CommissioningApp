using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Data;

namespace WpfCommApp
{
    /// <summary>
    /// This is a default class that I use to debug bindings when they are not functioning properly
    /// </summary>
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

    /// <summary>
    /// Class used to select the proper string from an array of arrays and display to the user in the 
    /// CommissioningView
    /// </summary>
    public class StringArrayToStringConverter : IMultiValueConverter
    {
        /// <summary>
        /// Indexes into an array of arrays of strings in order to retrieve a string and display to user
        /// </summary>
        /// <param name="values">First item is an array of array of strings, the second item is an index into the second array</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] as string[,] == null)
                return string.Empty;

            return (values[0] as string[,])[(int)values[1] - 1, (int)values[2]];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This is used to generate the not equal value for the Reason field within a channel
    /// </summary>
    public class NC : IValueConverter
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

    /// <summary>
    /// Used to collapse an entire reason column if none of the values have been forced
    /// </summary>
    public class ColumnCollapse : IValueConverter
    {

        // Collapses entire column for Forced Reason if all of forced values for this phase are 
        // false
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return "Visible";

            foreach (Channel c in (value as ObservableCollection<Channel>))
                if (c.Forced[(int)parameter] == true && (c.Phase1 == true || c.Phase2 == true))
                    return "Visible";

            return "Collapsed";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
