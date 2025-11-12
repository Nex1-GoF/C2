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
        public bool IsVisible { get; private set; } = false;

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
        public void UpdateVisible(bool isVisible)
        {
            IsVisible = isVisible;
        }

        public void UpdateMissileInfo(Missile missile)
        {
            Id = _missile.Id;
            Yaw = (double)_missile.Yaw / 100.0;
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



        public PIPMarkerViewModel(PIP pip, string missileId)
        {
            MissileId = missileId;
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
        public Target Target { get; private set; }

        public string Id { get; private set; }
        public char DefaultID { get; private set; }
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
            Target = target;

            Id = $"TARGET-{(Target.Id):D3}";
            DefaultID = Target.Id;
            Yaw = (double)(Target.Yaw)/100.0;
            Latitude = Target.CurLoc.Lat;
            Longitude = Target.CurLoc.Lon;
            Altitude = Target.Altitude;

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
            Id = $"TARGET-{Target.Id:D3}";
            DefaultID = Target.Id;
            Yaw = Target.Yaw;
            Latitude = Target.CurLoc.Lat;
            Longitude = Target.CurLoc.Lon;
            Altitude = Target.Altitude;
        }
    }

}
