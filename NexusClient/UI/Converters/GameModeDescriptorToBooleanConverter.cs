namespace Nexus.Client.UI.Converters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;
    using Nexus.Client.Games;

    public class GameModeInfoToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is IGameModeDescriptor;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
