using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace C2.Views.Markers
{
    public partial class TargetMarker2 : UserControl
    {
        private static readonly Brush UnfocusedFill = new SolidColorBrush(Color.FromRgb(255,51,51));
        private static readonly Brush FocusedFill =  new SolidColorBrush(Color.FromRgb(51,255,51));
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
                TargetRect.Fill = FocusedFill;
                IdLabel.Foreground = FocusedFill;
                return;
            }
            TargetRect.Fill = UnfocusedFill;
            IdLabel.Foreground = UnfocusedFill;
        }

        public void SetVisible(bool isVisible)
        {
            this.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

    }
}
