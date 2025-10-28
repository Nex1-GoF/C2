using System;
using System.Globalization;
using System.Windows.Data;
using C2.Models;

namespace C2.Converters
{
    public class MissileStateToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MissileState state)
            {
                return state switch
                {
                    MissileState.Standby => "대기중",
                    MissileState.InitialGuidance => "초기유도",
                    MissileState.MidGuidance => "중기유도",
                    MissileState.TerminalGuidance => "종말유도",
                    _ => "없음"
                };
            }
            return "없음";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
