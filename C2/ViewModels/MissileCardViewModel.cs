using C2.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;

namespace C2.ViewModels
{
    public partial class MissileCardViewModel : ObservableObject
    {
        private readonly Missile missile;

        [ObservableProperty] private string _id; // id
        [ObservableProperty] private string? _targetId; // 표적
        [ObservableProperty] private double _latitude; // 위도
        [ObservableProperty] private double _longitude; // 경도
        [ObservableProperty] private double _speed; // 고도
        [ObservableProperty] private MissileState _state; // 비행상태

        public MissileCardViewModel(Missile missile)
        {
            this.missile = missile;

            Id = missile.Id;
            TargetId = missile.TargetId;
            Latitude = missile.Latitude;
            Longitude = missile.Longitude;
            Speed = missile.Speed;
            State = missile.State;
        }

        

        // 비상폭파 로직 구현을 위한 테스트함수
        // 현재는 UI 업데이트를 확인하기위해 위도와 경도를 바꾸는 로직을 임시로 넣음
        [RelayCommand]
        private void Emergency()
        {
            // 위도, 경도를 각각 10 줄임
            Latitude -= 10;
            Longitude -= 10;

            // missile 모델에도 즉시 반영
            missile.Latitude = Latitude;
            missile.Longitude = Longitude;
        }

    }

}