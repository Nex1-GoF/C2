using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;

namespace C2.Models
{
    public enum MissileState : byte
    {
        LaunchReady = 1,
        InitialGuidance = 2,
        MidGuidance = 3,
        TerminalGuidance =4,
        Abort =5,
        Launching = 6,
    }
    public class PIP
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public short Altitude { get; set; }

        

        public PIP (double  lat, double lon, short alt)
        {
            Latitude = lat;
            Longitude = lon;
            Altitude = alt;
        }

    }
        public class Missile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private int _latitudeRaw;
        public int LatitudeRaw
        {
            get => _latitudeRaw;
            set
            {
                _latitudeRaw = value;

               
                OnPropertyChanged(nameof(Latitude)); // Latitude도 알림
            }
        }

        private int _longitudeRaw;
        public int LongitudeRaw
        {
            get => _longitudeRaw;
            set
            {
                _longitudeRaw = value;
                OnPropertyChanged(nameof(Longitude));
            }
        }
        public bool IsAbort { get; set; }
        public bool IsSelfabort { get; set; }
        public int Maxspeed = 200;
        private int _speed = 0;
        public int Speed
        {
            get => _speed;
            set { _speed = value; OnPropertyChanged(); }
        }

        private short _altitude;
        public short Altitude
        {
            get => _altitude;
            set { _altitude = value; OnPropertyChanged(); }
        }

        private MissileState _state;
        public MissileState State
        {
            get => _state;
            set { _state = value; OnPropertyChanged(); }
        }
        private int _yawRaw;
        public int YawRaw
        {
            get => _yawRaw;
            set
            {
                _yawRaw = value;
                OnPropertyChanged(nameof(Yaw)); // Latitude도 알림
                ;
            }
        }
        public short PitchRaw { get; set; }
        public uint FlightTime { get; set; }

        public string Id { get; set; }
        private string? _targetId;
        public string? TargetId
        {
            get => _targetId;
            set
            {
                if (_targetId != value)
                {
                    _targetId = value;
                    OnPropertyChanged(nameof(TargetId));
                    OnPropertyChanged(nameof(TargetDisplayId));
                }
            }
        }

        public string TargetDisplayId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TargetId))
                    return "";

                // TargetId → 정수 변환 시도
                if (int.TryParse(TargetId, out int id))
                    return $"TGT-{id:000}";

                return "";
            }
        }
        private int? _ramainingDistance;
        public int? RemainingDistance
        {
            get => _ramainingDistance;
            set
            {
                if (_ramainingDistance != value)
                {
                    _ramainingDistance = value;
                    OnPropertyChanged(nameof(RemainingDistance));
                    OnPropertyChanged(nameof(DisplayRemainingDistance));
                }
            }
        }

        public string DisplayRemainingDistance
        {
            get
            {
                if (RemainingDistance == null)
                    return "";

                return $"{RemainingDistance}";
            }
        }

        public int IdNumber => int.Parse(Id);
        public double Latitude => LatitudeRaw / 1e7;
        public double Longitude => LongitudeRaw / 1e7;
        public double Yaw => YawRaw / 100.0;
        public double Pitch => PitchRaw / 100.0;
        public List<(double Lat, double Lon)> PathHistory { get; } = new();
        public PIP? PIP { get; set; }
        public ulong flightTime;
        public Missile(
            string id,
            int latitudeRaw,
            int longitudeRaw,
            short altitude,
            ushort yawRaw,
            short pitchRaw,
            uint flightTime,
            MissileState state,
            int speed = 0,
            string? targetId = null,
            int? remainingDistance = null)
        {
            Id = id;
            LatitudeRaw = latitudeRaw;
            LongitudeRaw = longitudeRaw;
            Altitude = altitude;
            YawRaw = yawRaw;
            PitchRaw = pitchRaw;
            FlightTime = flightTime;
            State = state;
            TargetId = targetId;
            RemainingDistance = remainingDistance;
            Speed = speed;
            flightTime = 0;
        }
        public Missile(
            string id,
            int latitudeRaw,
            int longitudeRaw,
            short altitude,
            ushort yawRaw,
            short pitchRaw,
            uint flightTime,
            MissileState state,
            PIP pip,
            int speed = 0,
            string? targetId = null,
            int? remainingDistance = null)
        {
            Id = id;
            LatitudeRaw = latitudeRaw;
            LongitudeRaw = longitudeRaw;
            Altitude = altitude;
            YawRaw = yawRaw;
            PitchRaw = pitchRaw;
            FlightTime = flightTime;
            State = state;
            TargetId = targetId;
            RemainingDistance = remainingDistance;
            Speed = speed;
            flightTime = 0;
            PIP = pip;
        }

        public Missile(string id, int latitudeRaw, int longitudeRaw, short altitude, int speed)
        {
            Id = id;
            LatitudeRaw = latitudeRaw;
            LongitudeRaw = longitudeRaw;
            Altitude = altitude;
            Speed = speed;
            State = MissileState.LaunchReady;
            IsAbort = false;
            IsSelfabort = false;
        }

        public void Update(Missile src)
        {
            LatitudeRaw = src.LatitudeRaw;
            LongitudeRaw = src.LongitudeRaw;
            Altitude = src.Altitude;

            YawRaw = src.YawRaw;
            PitchRaw = src.PitchRaw;
            Speed = src.Speed;
            FlightTime = src.FlightTime;
            State = src.State;
            PIP = src.PIP;
            RemainingDistance = src.RemainingDistance;
            //TargetId = src.TargetId;
        }
    }
}