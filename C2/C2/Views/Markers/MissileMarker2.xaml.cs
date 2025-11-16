using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace C2.Views.Markers
{
    public partial class MissileMarker2 : UserControl
    {
        private static readonly Brush UnfocusedStroke = new SolidColorBrush(Colors.LimeGreen);
        private static readonly Brush UnfocusedFill = new SolidColorBrush(Colors.LightGreen);

        private static readonly Brush FocusedStroke = new SolidColorBrush(Colors.LimeGreen);
        private static readonly Brush FocusedFill = new SolidColorBrush(Colors.Red);

        public string MissileId { get; }

        public MissileMarker2(string missileId)
        {
            MissileId = missileId;
            InitializeComponent();
            SetFocused(false);
        }

        public void SetYaw(double yaw)
        {
            Rt.Angle = yaw;
        }

        public void SetFocused(bool focused)
        {
            if (focused)
            {
                MissileRect.Stroke = FocusedStroke;
                MissileRect.Fill = FocusedFill;
                return;
            }
            MissileRect.Stroke = UnfocusedStroke;
            MissileRect.Fill = UnfocusedFill;
        }

        public void SetVisible(bool isVisible)
        {
            this.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public void SetColor(Brush fill, Brush stroke)
        {
            MissileRect.Fill = fill;
            MissileRect.Stroke = stroke;
        }
    }
}
