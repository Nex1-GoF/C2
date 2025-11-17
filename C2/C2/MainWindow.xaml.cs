using C2.Models;
using C2.Network;
using C2.Services;
using C2.ViewModels;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
namespace C2
{
    public partial class MainWindow : Window
    {
        private const double MapExpandRatio = 0.9;
        private const double MapDefaultRatio = 0.75;

        private readonly MainViewModel _vm;
        private TargetReceiver _targetReceiver;
        private MissileReceiver _missileReceiver;
        private AbortManager _abortManager;
        private SocketManager _socketManager;

        public MainWindow()
        {
            InitializeComponent();
            MissilePanelRef.CollapseToggled += OnMissileCollapseChanged;
            _vm = new MainViewModel();
            DataContext = _vm;

            NativeMethods.AllocConsole();

            _socketManager = SocketManager.Instance;
            _socketManager.Initialize();

            _targetReceiver = new TargetReceiver(_socketManager);
            _missileReceiver = new MissileReceiver(_socketManager);
            _abortManager = AbortManager.Instance;
        }

        // 로그 확인용 - 콘솔 창 열기
        static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AllocConsole();
        }
        private void GlobalCollapseButton_Click(object sender, RoutedEventArgs e)
        {
            // 🔹 MissilePanel의 동일 로직 실행
            MissilePanelRef.ToggleCollapse();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
        }

        private void OnMissileCollapseChanged(bool isCollapsed)
        {
            AnimatePanelResize(isCollapsed);
        }

        private void AnimatePanelResize(bool isCollapsed)
        {
            var mapRow = LeftGrid.RowDefinitions[0];
            var missileRow = LeftGrid.RowDefinitions[1];

            // 🔹 고정 비율 정의
            const double MapDefaultRatio = 0.75;
            const double MapExpandRatio = 0.9;

            double currentMapStar = mapRow.Height.Value;
            double currentMissileStar = missileRow.Height.Value;

            double toMapRatio = isCollapsed ? MapExpandRatio : MapDefaultRatio;
            double toMissileRatio = 1 - toMapRatio;

            // 🔹 Star 단위 그대로 보간하도록 설정
            var animMap = new GridLengthAnimation
            {
                From = new GridLength(currentMapStar, GridUnitType.Star),
                To = new GridLength(toMapRatio, GridUnitType.Star),
                Duration = TimeSpan.FromMilliseconds(300)
            };

            var animMissile = new GridLengthAnimation
            {
                From = new GridLength(currentMissileStar, GridUnitType.Star),
                To = new GridLength(toMissileRatio, GridUnitType.Star),
                Duration = TimeSpan.FromMilliseconds(300)
            };

            mapRow.BeginAnimation(RowDefinition.HeightProperty, animMap);
            missileRow.BeginAnimation(RowDefinition.HeightProperty, animMissile);

            // ❌ DataContext 재설정 금지
            // DataContext = new MainViewModel(); (삭제)
        }

        private void TargetPanel_Loaded(object sender, RoutedEventArgs e)
        {

        }
    }

    public class GridLengthAnimation : AnimationTimeline
    {
        public override Type TargetPropertyType => typeof(GridLength);

        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }
        public static readonly DependencyProperty FromProperty =
            DependencyProperty.Register(nameof(From), typeof(GridLength), typeof(GridLengthAnimation));

        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }
        public static readonly DependencyProperty ToProperty =
            DependencyProperty.Register(nameof(To), typeof(GridLength), typeof(GridLengthAnimation));

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock clock)
        {
            double fromVal = From.Value;
            double toVal = To.Value;
            double progress = clock.CurrentProgress ?? 0;

            // 🔹 항상 비율 단위 기준으로 보간
            double currentVal = fromVal + (toVal - fromVal) * progress;
            return new GridLength(currentVal, GridUnitType.Star);
        }

        protected override Freezable CreateInstanceCore() => new GridLengthAnimation();
    }

    public class BoolToTextConverter : IValueConverter
    {
        // parameter: "발사|발사취소"
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is string param && param.Contains('|'))
            {
                var parts = param.Split('|');
                if (value is bool b)
                    return b ? parts[1] : parts[0];
            }
            return "발사";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
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

    public class StepCompletedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            string stepName = values[0] as string;
            string currentStep = values[1] as string;
            var steps = values[2] as IList<string>;

            if (stepName == null || currentStep == null || steps == null)
                return false;

            int stepIndex = steps.IndexOf(stepName);
            int currentIndex = steps.IndexOf(currentStep);

            return stepIndex <= currentIndex;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}