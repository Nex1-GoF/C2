using C2.Models;
using C2.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace C2.Views
{
    public partial class MissilePanel : UserControl
    {
        public event Action<bool> CollapseToggled;

        public MissilePanel()
        {
            InitializeComponent();
        }

        // 🔹 내부 버튼 (기존과 동일)
        public void CollapseButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleCollapse();
        }

        // 🔹 외부(MainWindow)에서도 동일 동작하도록 공개 메서드로 분리
        public void ToggleCollapse()
        {
            if (DataContext is MissileViewModel vm)
            {
                vm.IsCollapsed = !vm.IsCollapsed;
                CollapseToggled?.Invoke(vm.IsCollapsed);

                // ✅ 강제로 ItemsControl 갱신
                MissileItemsControl.ItemTemplateSelector = null;
                MissileItemsControl.ItemTemplateSelector =
                    (MissileTemplateSelector)FindResource("MissileTemplateSelector");
            }
        }
    }

    // 템플릿 선택자
    public class MissileTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ExpandedTemplate { get; set; }
        public DataTemplate CompactTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var window = Application.Current.MainWindow as C2.MainWindow;
            var missilePanel = window?.MissilePanelRef;
            if (missilePanel?.DataContext is MissileViewModel vm)
            {
                return vm.IsCollapsed ? CompactTemplate : ExpandedTemplate;
            }
            return ExpandedTemplate;
        }
    }

    // 텍스트 변환기
    public class BooleanToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string param)
            {
                var parts = param.Split('|');
                if (parts.Length == 2 && value is bool b)
                    return b ? parts[1] : parts[0];
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    // 상태 → 텍스트
    public class StateToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MissileState state)
            {
                return state switch
                {
                    MissileState.LaunchReady => "대기",
                    MissileState.Launching => "발사 중",
                    MissileState.InitialGuidance => "초기유도",
                    MissileState.MidGuidance => "중기유도",
                    MissileState.TerminalGuidance => "종말유도",
                    MissileState.Abort => "중단",
                    _ => "대기"
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    // 상태 → 색상
    public class StateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MissileState state)
            {
                return state switch
                {
                    MissileState.LaunchReady => Brushes.SkyBlue,
                    MissileState.Launching => new SolidColorBrush(Color.FromRgb(0x66, 0xCC, 0xFF)),
                    MissileState.InitialGuidance => Brushes.Yellow,
                    MissileState.MidGuidance => Brushes.Orange,
                    MissileState.TerminalGuidance => Brushes.Red,
                    MissileState.Abort => Brushes.Gray,
                    _ => Brushes.SkyBlue
                };
            }
            return Brushes.SkyBlue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class StateToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MissileState state)
                return state == MissileState.MidGuidance;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}