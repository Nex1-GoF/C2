using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using C2.Services;

namespace C2.ViewModels
{
    public partial class MissilePanelViewModel : ObservableObject
    {
        private readonly MissileService _missileService;
        private readonly DispatcherTimer _updateTimer;

        public ObservableCollection<MissileCardViewModel> Missiles { get; } = new();

        public MissilePanelViewModel()
        {
            _missileService = MissileService.Instance;

            // 초기 데이터
            foreach (var ctrl in _missileService.missileControllers)
                Missiles.Add(new MissileCardViewModel(ctrl.Missile));

            // 주기적 갱신
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _updateTimer.Tick += (s, e) => RefreshMissiles();
            _updateTimer.Start();
        }

        private void RefreshMissiles()
        {
            for (int i = 0; i < Missiles.Count; i++)
            {
                var vm = Missiles[i];
                var m = _missileService.missileControllers[i].Missile;

                vm.Latitude = m.Latitude;
                vm.Longitude = m.Longitude;
                vm.State = m.State;
            }
        }
    }
}
