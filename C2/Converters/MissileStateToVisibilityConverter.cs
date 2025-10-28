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
    ///   - "midterm"   → 위치/자세/비행정보용
    ///   - "emergency" → 비상 폭파 버튼 표시용
    /// </summary>
    public class MissileStateToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return Visibility.Collapsed;

            if (values[0] is not MissileState state)
                return Visibility.Collapsed;

            var targetId = values[1] as string;
            var param = (parameter as string)?.ToLower() ?? string.Empty;

            switch (param)
            {
                case "target":
                    // ✅ 표적 정보는 LaunchReady 이상에서 TargetId가 있을 때 표시
                    return (state != MissileState.Abort && !string.IsNullOrWhiteSpace(targetId))
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                case "midterm":
                    // ✅ 위도/경도/고도/요/피치 등은 중기~종말 유도 시 표시
                    return (state == MissileState.MidGuidance ||
                            state == MissileState.TerminalGuidance)
                        ? Visibility.Visible
                        : Visibility.Collapsed;

                case "emergency":
                    // ✅ 비상 폭파 버튼도 중기~종말 유도 시에만 표시
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
