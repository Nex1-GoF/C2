using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace C2.Views.Markers
{
    public partial class TargetMarker2 : UserControl
    {
        private static readonly Brush UnfocusedStroke = new SolidColorBrush(Colors.Red);
        private static readonly Brush UnfocusedFill = new SolidColorBrush(Color.FromRgb(0xFF, 0x40, 0x40));

        private static readonly Brush FocusedStroke = new SolidColorBrush(Colors.Blue);
        private static readonly Brush FocusedFill = new SolidColorBrush(Color.FromRgb(0xFF, 0x40, 0x40));
        public char TargetId { get; }
        public TargetMarker2(char targetId)
        {
            InitializeComponent();
            TargetId = targetId;

            IdLabel.Text = $"TGT-00{targetId}";
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
                TargetRect.Stroke = FocusedStroke;
                TargetRect.Fill = FocusedFill;
                return;
            }
            TargetRect.Stroke = UnfocusedStroke;
            TargetRect.Fill = UnfocusedFill;
        }

        public void SetVisible(bool isVisible)
        {
            this.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

    }
}
