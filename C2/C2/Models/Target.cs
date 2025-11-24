using C2.Services;
using Newtonsoft.Json.Linq;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace C2.Models
{
    public enum TargetState : byte
    {
        Unknown = 0,
        Guidance,
        Terminate
    }

    public class Target : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public char Id { get; set; }

        private int _speed;
        public int Speed { get => _speed; set { _speed = value; OnPropertyChanged(); } }

        private int _altitude;
        public int Altitude { get => _altitude; set { _altitude = value; OnPropertyChanged(); } }

        private int _yaw;
        public int Yaw { get => _yaw; set { _yaw = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(YawRaw));
            } }

        public double YawRaw => (double)Yaw / 100.0;
        

        private TargetState _state;
        public TargetState State { get => _state; set { _state = value; OnPropertyChanged(); } }

        private (double Lat, double Lon) _curLoc;
        public (double Lat, double Lon) CurLoc
        {
            get => _curLoc;
            set { _curLoc = value; OnPropertyChanged(); OnPropertyChanged(nameof(CurLatDisplay)); OnPropertyChanged(nameof(CurLonDisplay)); }
        }

        private (double Lat, double Lon) _endLoc;
        public (double Lat, double Lon) EndLoc
        {
            get => _endLoc;
            set { _endLoc = value; OnPropertyChanged(); OnPropertyChanged(nameof(EndLocDisplay)); }
        }

        public DateTime DetectTime { get; set; }

        public string CurYawDisplay => $"{(Yaw)/100:F0}°";
        public string CurLatDisplay => $"{CurLoc.Lat:F5}";
        public string CurLonDisplay => $"{CurLoc.Lon:F5}";
        public string EndLocDisplay => $"{EndLoc.Lat:F5}, {EndLoc.Lon:F5}";

        public List<(double Lat, double Lon)> PathHistory { get; } = new();

        public string TargetInfoName { get; set; }
        public string TargetInfoImagePath { get; set; }

        public Target(char id, int speed, int altitude, int yaw, (double Lat, double Lon) endLoc, DateTime detectTime, (double Lat, double Lon) curLoc, char detectedType)
        {
            Id = id;
            Speed = speed;
            Altitude = altitude;
            Yaw = yaw;
            State = TargetState.Unknown;
            EndLoc = endLoc;
            DetectTime = detectTime;
            CurLoc = curLoc;

            TargetInfo targetInfo = TargetInfos.Instance.GetTargetInfoById(detectedType);
            TargetInfoName = targetInfo.TargetName;
            TargetInfoImagePath = targetInfo.ImagePath;

        }

        public void Update(Target updated)
        {
            Speed = updated.Speed;
            Altitude = updated.Altitude;
            Yaw = updated.Yaw;
            CurLoc = updated.CurLoc;
            EndLoc = updated.EndLoc;
            DetectTime = updated.DetectTime;
        }
    }




    class TargetInfo
    {
        public char Id { get; set; }
        public string TargetName { get; set; }
        public string ImagePath { get; set; }

        public TargetInfo(char id, string targetName, string imagePath)
        {
            Id = id;
            TargetName = targetName;
            ImagePath = imagePath;
        }
    }

    class TargetInfos
    {
        private static TargetInfos _instance;
        public static TargetInfos Instance => _instance ??= new TargetInfos();

        Dictionary<char, TargetInfo> targetInfos = new();
        private TargetInfos()
        {
            targetInfos.Add('0', new TargetInfo('0', "UNKNOWN", "/Resources/target2.png"));
            targetInfos.Add('A', new TargetInfo('A', "MIG29", "/Resources/target5.png"));
            targetInfos.Add('B', new TargetInfo('B', "MIG15", "/Resources/target5.png"));
            targetInfos.Add('C', new TargetInfo('C', "SU25", "/Resources/target2.png"));
        }

        public TargetInfo GetTargetInfoById(char id) {
            if (targetInfos.ContainsKey(id)) return targetInfos[id];
            else return targetInfos['0'];
        }
    }
}