using C2.Models;
using C2.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace C2.Views
{
    /// <summary>
    /// TargetPanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TargetPanel : UserControl
    {
        private readonly TargetViewModel _vm;
        public TargetPanel()
        {
            InitializeComponent();
            _vm = new TargetViewModel();
            DataContext = _vm;
        }



        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 버튼 클릭 시 카드 클릭이 실행되지 않도록
            if (e.Handled)
                return;

            if (sender is Border border && border.DataContext is Target target)
            {
                var vm = DataContext as TargetViewModel;
                if (vm?.TargetCardClickCommand?.CanExecute(target) == true)
                    vm.TargetCardClickCommand.Execute(target);
            }
        }
        private void EngagementButton_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;  // 카드 클릭으로 전달되지 않도록

            if (sender is Button btn && btn.DataContext is Target target)
            {
                var vm = DataContext as TargetViewModel;
                if (vm?.EngagementCommand?.CanExecute(target) == true)
                    vm.EngagementCommand.Execute(target);
            }
        }
        
    }
    public class TargetStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TargetState state)
            {
                return state switch
                {
                    TargetState.Unknown => Brushes.Gray,
                    TargetState.Guidance => Brushes.Yellow,
                    TargetState.Terminate => Brushes.Red,
                    _ => Brushes.Gray
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class TargetFocusedToBackgroundConverter : IMultiValueConverter
    {
        private static Brush NormalBrush =>
            (Brush)Application.Current.FindResource("TargetCardBrush");

        private static Brush FocusedBrush =>
            (Brush)Application.Current.FindResource("TargetCardFocusedHeaderBrush");

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2)
                return NormalBrush;

            var current = values[0];
            var selected = values[1];

            if (current != null && selected != null && current == selected)
                return FocusedBrush;

            return NormalBrush;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
