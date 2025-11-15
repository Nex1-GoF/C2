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
using System.Linq;
using System.Windows.Input;

namespace C2.ViewModels
{
    public partial class MissileViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isCollapsed = false;

        [ObservableProperty]
        private ObservableCollection<Missile> missiles = new();

        private readonly MissileService _service;
        private readonly AbortManager _abortManager;

        // ✅ 여러 미사일 선택 가능하도록 리스트화
        private readonly List<Missile> _selectedMissiles = new();
        public IReadOnlyList<Missile> SelectedMissiles => _selectedMissiles;
        public MissileViewModel()
        {
            _service = MissileService.Instance;
            _abortManager = AbortManager.Instance;

            // ✅ 초기 1회만 미사일 4기 채움
            foreach (var m in _service.GetAllMissiles())
                Missiles.Add(m);

           // UpdateDispatcher.Instance.Register(UpdateMissileStates);
        }
        ~MissileViewModel()
        {
            //UpdateDispatcher.Instance.Unregister(UpdateMissileStates);
        }

        [RelayCommand]
        private void ToggleCollapse()
        {
            IsCollapsed = !IsCollapsed;
        }

        [RelayCommand]
        private void Abort(Missile missile)
        {
            missile.State = MissileState.Abort;
            missile.IsSelfabort = true;
            var mslId = $"M{int.Parse(missile.Id):000}";
            _abortManager.AbortMissile(mslId);

            // TODO: 폭파 로직 추가
            // TODO: DatalinkService의 비상폭파 로직 실행
        }

        [RelayCommand]
        public void SelectMissile(Missile missile)
        {
            if (missile == null)
            {
                _selectedMissiles.Clear();
                _service.ClearMissiles(); // 다중 선택 해제용
                return;
            }

            // ✅ 이미 선택되어 있다면 해제
            if (_selectedMissiles.Contains(missile))
            {
                _selectedMissiles.Remove(missile);
            }
            else
            {
                _selectedMissiles.Add(missile);
            }

            // ✅ Service에도 반영
            var ids = _selectedMissiles.Select(m => m.Id).ToList();
            _service.SelectMissiles(ids);
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
        private static double CalculateYaw(double lat1, double lon1, double lat2, double lon2)
        {
            double φ1 = lat1 * Math.PI / 180.0;
            double φ2 = lat2 * Math.PI / 180.0;
            double Δλ = (lon2 - lon1) * Math.PI / 180.0;

            double y = Math.Sin(Δλ) * Math.Cos(φ2);
            double x = Math.Cos(φ1) * Math.Sin(φ2) -
                       Math.Sin(φ1) * Math.Cos(φ2) * Math.Cos(Δλ);

            double θ = Math.Atan2(y, x);
            double bearing = (θ * 180.0 / Math.PI + 360.0) % 360.0;
            return bearing;
        }
    }
}
