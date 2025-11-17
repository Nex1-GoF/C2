using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace C2.Views.Markers
{
    public partial class MissileMarker2 : UserControl
    {
        private static readonly Brush UnfocusedFill = new SolidColorBrush(Colors.LimeGreen);
        private static readonly Brush FocusedFill = new SolidColorBrush(Colors.SkyBlue);
        private static readonly Brush LaunchingFill = new SolidColorBrush(Colors.Orange);

        public string MissileId { get; }
        public bool IsLaunching { get; set; } = false;

        public MissileMarker2(string missileId)
        {
            MissileId = missileId;
            InitializeComponent();

            IdLabel.Text = $"MSL-00{missileId}";
            SetColor(false);
        }

        public void SetYaw(double yaw)
        {
            Rt.Angle = yaw;
        }

        public void SetColor(bool focused)
        {
            if (IsLaunching)
            {
                MissileRect.Fill = LaunchingFill;
                IdLabel.Foreground = LaunchingFill;
                return;
            }
            if (focused)
            {
                MissileRect.Fill = FocusedFill;
                IdLabel.Foreground = FocusedFill;
                return;
            }
            MissileRect.Fill = UnfocusedFill;
            IdLabel.Foreground = UnfocusedFill;
        }

        public void SetVisible(bool isVisible)
        {
            this.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetLaunching(bool isLaunching)
        {
            IsLaunching = isLaunching;
        }

        /*        public void SetColor(Brush fill, Brush stroke)
                {
                    MissileRect.Fill = fill;
                    MissileRect.Stroke = stroke;
                }

                public void SetLaunchingColor()
                {
                    MissileRect.Stroke = LaunchingStroke;
                    MissileRect.Fill = LaunchingFill;
                }*/
    }
}
