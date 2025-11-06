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
        public TargetPanel()
        {
            InitializeComponent();

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
}
