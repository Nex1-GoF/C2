using C2.Models;
using C2.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
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

        private void MissileCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled)
                return;

            if (sender is Border border && border.DataContext is Missile missile)
            {
                var vm = DataContext as MissileViewModel;
                if (vm?.SelectMissileCommand?.CanExecute(missile) == true)
                    vm.SelectMissileCommand.Execute(missile);
            }
        }

        private void AbortButton_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // 카드 클릭으로 전달되는 것 막기
            if (sender is Button btn && btn.DataContext is Missile missile)
            {
                var vm = DataContext as MissileViewModel;
                if (vm?.AbortCommand?.CanExecute(missile) == true)
                    vm.AbortCommand.Execute(missile);
            }
        }

        private void LocalCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleCollapse();
        }

        public void ToggleCollapse()
        {
            if (DataContext is not MissileViewModel vm)
                return;

            vm.IsCollapsed = !vm.IsCollapsed;

            // 🔥 ItemsControl 강제 Refresh (완전 안전한 방식)
            CollapseToggled?.Invoke(vm.IsCollapsed);
            RefreshItemsControl();
        }

        private void RefreshItemsControl()
        {
            var items = MissileItemsControl.ItemsSource;

            // ⭐ ItemsSource 잠시 끊었다가 다시 연결하면 TemplateSelector 재실행됨
            MissileItemsControl.ItemsSource = null;
            MissileItemsControl.ItemsSource = items;
        }
    }

    // 템플릿 선택자
    public class MissileTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ExpandedTemplate { get; set; }
        public DataTemplate CompactTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement fe)
            {
                // 🔥 UserControl(MissilePanel) 찾아 올라감
                var panel = FindParent<MissilePanel>(fe);
                if (panel?.DataContext is MissileViewModel vm)
                {
                    return vm.IsCollapsed ? CompactTemplate : ExpandedTemplate;
                }
            }

            return ExpandedTemplate;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && parent is not T)
                parent = VisualTreeHelper.GetParent(parent);

            return parent as T;
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
    public class CollapseIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool collapsed = value is bool b && b;
            return Application.Current.MainWindow.FindResource(
                collapsed ? "IconExpand" : "IconCollapse"
            );
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }


    public class StateToEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MissileState state)
                return (state == MissileState.MidGuidance || state == MissileState.TerminalGuidance);

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class FocusedToBackgroundConverter : IMultiValueConverter
    {
        private static readonly Brush FocusedBrush =
            (Brush)Application.Current.FindResource("MissileCardFocusedHeaderBrush");

        private static readonly Brush NormalBrush =
            (Brush)Application.Current.FindResource("MissileCardBrush");

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 같은 로직 유지
            if (values.Length < 2 ||
                values[0] is not Missile current ||
                values[1] is not IReadOnlyList<Missile> selectedList)
            {
                return NormalBrush;
            }

            // 선택 또는 포커스된 경우 → 강조색
            return selectedList.Contains(current) ? FocusedBrush : NormalBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class CoordinateStateMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] = 숫자값
            // values[1] = MissileState

            if (values.Length < 2)
                return "-";

            if (values[1] is not MissileState state)
                return "-";

            // 상태가 대기/중단이면 숨김
            if (state == MissileState.LaunchReady || state == MissileState.Abort)
                return "-";

            // 값이 null이면 숨김
            if (values[0] is null)
                return "-";

            // 숫자면 소수점 4자리 적용
            if (values[0] is double d)
                return d.ToString("F4");

            if (values[0] is float f)
                return f.ToString("F4");

            if (values[0] is int i)
                return i.ToString();  // 고도 같은 정수값은 그대로

            return values[0].ToString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class StateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MissileState state)
                return Application.Current.FindResource("LaunchReadyBrush");

            return state switch
            {
                MissileState.LaunchReady => Application.Current.FindResource("LaunchReadyBrush"),
                MissileState.Launching => Application.Current.FindResource("LaunchingBrush"),
                MissileState.InitialGuidance => Application.Current.FindResource("InitialGuidanceBrush"),
                MissileState.MidGuidance => Application.Current.FindResource("MidGuidanceBrush"),
                MissileState.TerminalGuidance => Application.Current.FindResource("TerminalGuidanceBrush"),
                MissileState.Abort => Application.Current.FindResource("AbortedBrush"),

                _ => Application.Current.FindResource("LaunchReadyBrush")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }


    public class StateToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not MissileState state)
                return "대기";

            return state switch
            {
                MissileState.LaunchReady => "대기",
                MissileState.Launching => "발사 중",
                MissileState.InitialGuidance => "초기유도",
                MissileState.MidGuidance => "중기유도",
                MissileState.TerminalGuidance => "종말유도",
                MissileState.Abort => "중단됨",

                _ => "대기"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}