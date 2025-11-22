using C2.Messages;
using C2.Models;
using C2.Network;
using C2.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace C2.ViewModels
{
    public partial class MissileViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isCollapsed = false;

        [ObservableProperty]
        private List<Missile> missiles = new();

        private readonly MissileService _service;
        private readonly TargetService _targetService;
        private readonly AbortManager _abortManager;

        // 여러 미사일 선택 가능하도록 리스트화
        private List<Missile> _selectedMissiles = new();
        public List<Missile> SelectedMissiles
        {
            get => _selectedMissiles;
            private set => SetProperty(ref _selectedMissiles, value);
        }

        public MissileViewModel()
        {
            _service = MissileService.Instance;
            _abortManager = AbortManager.Instance;

            // 초기 1회만 미사일 4기 채움
            // 서비스와 같은 생성자로 똑같이 미사일 객체를 생성 (서비스 레이어의 미사일 레퍼런스를 가지고올 경우, 옵저버블 컬랙션이랑 다를게 없어짐)
            for (int i = 1; i <= 4; i++)
            {
                var missile = new Missile(
                    id: $"{i:0}",
                    latitudeRaw: _service.C2Points.latitude,
                    longitudeRaw: _service.C2Points.longitude,
                    altitude: _service.C2Points.altitude,
                    speed: 1000
                );
                missiles.Add(missile);
            }

            UpdateDispatcher.Instance.Register(UpdateMissileStates);
        }
        ~MissileViewModel()
        {
            UpdateDispatcher.Instance.Unregister(UpdateMissileStates);
        }

        [RelayCommand]
        private void ToggleCollapse()
        {
            IsCollapsed = !IsCollapsed;
        }

        [RelayCommand]
        private void Abort(Missile missile)
        {
            missile.IsSelfabort = true;
            _abortManager.AbortMissile(missile.Id);

            // TODO: 폭파 로직 추가
            // TODO: DatalinkService의 비상폭파 로직 실행
        }

        [RelayCommand]
        public void SelectMissile(Missile missile)
        {
            if (missile == null)
            {
                SelectedMissiles = new List<Missile>();     // 여기서 UI 갱신됨
                _service.ClearMissiles();
                TargetService.Instance.ClearTarget();
                return;
            }

            // 이미 선택된 미사일이면 → 선택 해제
            if (_selectedMissiles.Contains(missile))
            {
                SelectedMissiles = new List<Missile>();     // 참조 변경 → UI 갱신
                _service.ClearMissiles();
                TargetService.Instance.ClearTarget();
                return;
            }

            // 전체 초기화
            SelectedMissiles = new List<Missile>();         // 참조 변경 → UI 갱신
            _service.ClearMissiles();
            TargetService.Instance.ClearTarget();

            // 새로운 리스트로 다시 선택 세팅
            SelectedMissiles = new List<Missile> { missile }; // 참조 변경 → UI 갱신
            _service.SelectMissiles(new List<string> { missile.Id });

            // 타겟 자동 선택
            if (missile.TargetId != null && missile.TargetId.Length > 0)
            {
                char targetId = missile.TargetId[0];
                TargetService.Instance.SelectTarget(targetId);
            }
        }



        private void UpdateMissileStates()
        {
            var serviceMissiles = _service.GetAllMissiles();

            for (int i = 0; i < Missiles.Count && i < serviceMissiles.Count; i++)
            {
                var local = Missiles[i];
                var latest = serviceMissiles[i];

                // 필요한 속성만 업데이트
                local.State = latest.State;
                local.LatitudeRaw = latest.LatitudeRaw;
                local.LongitudeRaw = latest.LongitudeRaw;
                local.Altitude = latest.Altitude;
                local.TargetId = latest.TargetId;
                // 미사일 모델의 Yaw 갱신
                local.YawRaw = latest.YawRaw;
            }
            // Todo: 미사일 비상폭파 기능 제한 걸기
        }
    }

}
