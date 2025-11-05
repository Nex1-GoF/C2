using C2.Services;
using C2.ViewModels;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
namespace C2
{
    public partial class MainWindow : Window
    {
        private const double MapExpandRatio = 0.85;
        private const double MapDefaultRatio = 0.7;

        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            MissilePanelRef.CollapseToggled += OnMissileCollapseChanged;
            _vm = new MainViewModel();
            DataContext = _vm;
            
        }

        private void OnMissileCollapseChanged(bool isCollapsed)
        {
            AnimatePanelResize(isCollapsed);
        }

        private void AnimatePanelResize(bool isCollapsed)
        {
            var mapRow = LeftGrid.RowDefinitions[0];
            var missileRow = LeftGrid.RowDefinitions[1];

            double fromMap = mapRow.ActualHeight;
            double total = LeftGrid.ActualHeight;
            if (total == 0) return;

            double toMapRatio = isCollapsed ? MapExpandRatio : MapDefaultRatio;
            double toMissileRatio = 1 - toMapRatio;

            // GridLengthAnimation 기반 비율 조정
            var animMap = new GridLengthAnimation
            {
                From = mapRow.Height,
                To = new GridLength(toMapRatio, GridUnitType.Star),
                Duration = TimeSpan.FromMilliseconds(300)
            };
            var animMissile = new GridLengthAnimation
            {
                From = missileRow.Height,
                To = new GridLength(toMissileRatio, GridUnitType.Star),
                Duration = TimeSpan.FromMilliseconds(300)
            };

            mapRow.BeginAnimation(RowDefinition.HeightProperty, animMap);
            missileRow.BeginAnimation(RowDefinition.HeightProperty, animMissile);
            DataContext = new MainViewModel(); // ✅ ViewModel 연결

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

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            double fromVal = ((GridLength)From).Value;
            double toVal = ((GridLength)To).Value;
            double progress = animationClock.CurrentProgress ?? 0;

            double currentVal = fromVal + (toVal - fromVal) * progress;
            return new GridLength(currentVal, GridUnitType.Star);
        }

        protected override Freezable CreateInstanceCore() => new GridLengthAnimation();
    }
}