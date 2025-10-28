using C2.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace C2.ViewModels
{
    public partial class MissileCardViewModel : ObservableObject
    {
        [ObservableProperty] private string _id; // id
        [ObservableProperty] private string? _targetId; // 표적
        [ObservableProperty] private double _latitude; // 위도
        [ObservableProperty] private double _longitude; // 경도
        [ObservableProperty] private double _speed; // 고도
        [ObservableProperty] private MissileState _state; // 비행상태

        public MissileCardViewModel(Missile missile)
        {
            Id = missile.Id;
            TargetId = missile.TargetId;
            Latitude = missile.Latitude;
            Longitude = missile.Longitude;
            Speed = missile.Speed;
            State = missile.State;
        }

        //public string DisplayStatus => 
        //    State switch
        //    {
        //        MissileState.Standby => "대기중",
        //        MissileState.InitialGuidance => "초기유도",
        //        MissileState.MidGuidance => "중기유도",
        //        MissileState.TerminalGuidance => "종말유도",
        //        _ => "알 수 없음"
        //    };

        //public string IconColor =>
        //    State switch
        //    {
        //        MissileState.Standby => "LimeGreen",
        //        MissileState.InitialGuidance => "Yellow",
        //        MissileState.MidGuidance => "Orange",
        //        MissileState.TerminalGuidance => "Red",
        //        _ => "Gray"
        //    };
    }

}