using C2.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace C2.ViewModels
{
    public partial class MissileCardViewModel : ObservableObject
    {
        private readonly Missile missile;

        [ObservableProperty] private string _id;               // 유도탄 ID
        [ObservableProperty] private string? _targetId;        // 표적 ID

        [ObservableProperty] private double _latitude;          // 위도 (°)
        [ObservableProperty] private double _longitude;         // 경도 (°)
        [ObservableProperty] private short _altitude;           // 고도 (m)
        [ObservableProperty] private double _yaw;               // 요 (°)
        [ObservableProperty] private double _pitch;             // 피치 (°)

        [ObservableProperty] private uint _flightTime;          // 비행시간 (ms)
        [ObservableProperty] private MissileState _state;       // 비행상태
        [ObservableProperty] private TelemetryState _telemetry; // 텔레메트리 상태

        public MissileCardViewModel(Missile missile)
        {
            this.missile = missile;

            Id = missile.Id;
            TargetId = missile.TargetId;

            Latitude = missile.Latitude;
            Longitude = missile.Longitude;
            Altitude = missile.Altitude;
            Yaw = missile.Yaw;
            Pitch = missile.Pitch;

            FlightTime = missile.FlightTime;
            State = missile.State;
            Telemetry = missile.Telemetry;
        }

        // 🚨 비상폭파 테스트 로직
        // 실제 폭파 로직이 아닌 UI 업데이트용
        [RelayCommand]
        private void Emergency()
        {
            // 위도/경도 10도 감소, 고도 100m 감소, 요/피치 각도 약간 변경
            Latitude -= 10;
            Longitude -= 10;

            // 모델에도 즉시 반영
            missile.LatitudeRaw = (int)(Latitude * 1e7);
            missile.LongitudeRaw = (int)(Longitude * 1e7);
        }
    }
}
