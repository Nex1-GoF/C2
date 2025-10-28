using C2.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using System;
using System.Collections.Generic;
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
    /// MissilePanel.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MissilePanel : UserControl
    {
        public MissilePanel()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetService<MissilePanelViewModel>();
        }
    }
}
