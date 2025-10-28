using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using C2.Models;

namespace C2.Converters
{
    /// <summary>
    /// MissileState와 TargetId를 함께 판단하여 Visibility 결정
    /// ConverterParameter:
    ///   - "target"    → 표적 표시용
    ///   - "midterm"   → 위도/경도/속도 표시용
    ///   - "emergency" → 비상 폭파 버튼 표시용
    /// </summary>
    public class MissileStateToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return Visibility.Collapsed;

            if (values[0] is not MissileState state)
                return Visibility.Collapsed;

            var targetId = values[1] as string;
            var param = parameter as string ?? string.Empty;

            switch (param.ToLower())
            {
                case "target":
                    // Standby 상태에서도 TargetId가 null이 아니면 표시
                    return (state == MissileState.InitialGuidance ||
                            state == MissileState.MidGuidance ||
                            state == MissileState.TerminalGuidance ||
                            (state == MissileState.Standby && !string.IsNullOrWhiteSpace(targetId)) ||
                            (state == MissileState.Standby))
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                case "midterm": // 위도/경도/속도
                    return (state == MissileState.MidGuidance ||
                            state == MissileState.TerminalGuidance)
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                case "emergency": // 비상 폭파 버튼
                    return (state == MissileState.MidGuidance ||
                            state == MissileState.TerminalGuidance)
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                default:
                    return Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
