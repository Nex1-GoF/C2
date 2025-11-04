using C2;
using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace C2.ViewModels
{
    public partial class MissileMarkerViewModel
    {
        private readonly Missile _missile;
        public string Id { get; private set; }
        public string? TargetId { get; private set; }
        public double Yaw { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public short Altitude { get; private set; }
        public Brush FillBrush { get; private set; }
        public Brush StrokeBrush { get; private set; }
        public bool IsFocused { get; private set; } = false;

        private readonly Brush _unFocusedBrush = new SolidColorBrush(Colors.LimeGreen);
        private readonly Brush _unFocusedFill = new SolidColorBrush(Colors.LightGreen);
        private readonly Brush _focusedBrush = new SolidColorBrush(Colors.SkyBlue);
        private readonly Brush _focusedFill = new SolidColorBrush(Colors.LightSkyBlue);


        public MissileMarkerViewModel(Missile missile)
        {
            _missile = missile;
            Id = _missile.Id;
            TargetId = _missile.TargetId;
            Yaw = _missile.Yaw;
            Longitude = _missile.Longitude;
            Latitude = _missile.Latitude;
            Altitude = _missile.Altitude;
            StrokeBrush = _unFocusedBrush;
            FillBrush = _unFocusedFill;
        }

        public void UpdateFocus(bool isFocused)
        {
            IsFocused = isFocused;
            if (isFocused)
            {
                StrokeBrush = _focusedBrush;
                FillBrush = _focusedFill;

            }
            else
            {
                StrokeBrush = _unFocusedBrush;
                FillBrush = _unFocusedFill;
            }
        }

        public void UpdateMissileInfo(Missile missile)
        {
            Id = _missile.Id;
            Yaw = _missile.Yaw;
            Longitude = _missile.Longitude;
            Latitude = _missile.Latitude;
            Altitude = _missile.Altitude;
        }

    }

    public class PIPMarkerViewModel
    {
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public string MissileId { get; private set; }
        public bool IsVisible { get; private set; } = false;



        public PIPMarkerViewModel(PIP pip)
        {
            MissileId = pip.MissileId;
            Latitude = pip.Latitude;
            Longitude = pip.Longitude;
        }

        public void UpdatePIP(PIP pip)
        {
            Latitude = pip.Latitude;
            Longitude = pip.Longitude;
        }


        public void UpdateVisible(bool visible) {
            IsVisible = visible;
        }

    }

    public partial class TargetMarkerViewModel
    {
        private readonly Target _target;

        public string Id { get; private set; }
        public string DefaultID { get; private set; }
        public double Yaw { get; private set; }
        public double Latitude { get; private set; }
        public double Longitude { get; private set; }
        public int Altitude { get; private set; }
        public Brush FillBrush { get; private set; }
        public Brush StrokeBrush { get; private set; }
        public bool IsFocused { get; private set; } = false;

        private readonly Brush _unFocusedBrush = new SolidColorBrush(Colors.Red);
        private readonly Brush _unFocusedFill = new SolidColorBrush(Color.FromRgb(0xFF, 0x40, 0x40));
        private readonly Brush _focusedBrush = new SolidColorBrush(Colors.Yellow);
        private readonly Brush _focusedFill = new SolidColorBrush(Colors.LightYellow);

        public TargetMarkerViewModel(Target target)
        {
            _target = target;

            Id = $"TARGET-{(_target.Id):D3}";
            DefaultID = _target.Id.ToString();
            Yaw = _target.Yaw;
            Latitude = _target.CurLoc.Lat;
            Longitude = _target.CurLoc.Lon;
            Altitude = _target.Altitude;

            StrokeBrush = _unFocusedBrush;
            FillBrush = _unFocusedFill;
        }

        public void UpdateFocus(bool isFocused)
        {
            IsFocused = isFocused;
            if (isFocused)
            {
                StrokeBrush = _focusedBrush;
                FillBrush = _focusedFill;

            }
            else
            {
                StrokeBrush = _unFocusedBrush;
                FillBrush = _unFocusedFill;
            }
        }

        public void UpdateTargetInfo(Target target)
        {
            Id = $"TARGET-{_target.Id:D3}";
            DefaultID = _target.Id.ToString();
            Yaw = _target.Yaw;
            Latitude = _target.CurLoc.Lat;
            Longitude = _target.CurLoc.Lon;
            Altitude = _target.Altitude;
        }
    }

}
