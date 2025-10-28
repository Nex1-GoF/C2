using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using C2.Models;

namespace C2.Converters
{
    public class MissileStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MissileState state)
            {
                return state switch
                {
                    MissileState.Standby => Brushes.LimeGreen,
                    MissileState.InitialGuidance => Brushes.Yellow,
                    MissileState.MidGuidance => Brushes.Orange,
                    MissileState.TerminalGuidance => Brushes.Red,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
