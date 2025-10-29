using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using C2.Messages;
using C2.Models;
using C2.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace C2.ViewModels
{
    public partial class MissilePanelViewModel : ObservableRecipient, IRecipient<MissileUpdateMessage>
    {
        public ObservableCollection<MissileCardViewModel> Missiles { get; }

        private readonly MissileService _missileService;

        public MissilePanelViewModel(MissileService missileService)
        {
            _missileService = missileService;

            // 수신 활성화
            IsActive = true;

            // MissileService로부터 미사일 목록 초기화
            Missiles = new ObservableCollection<MissileCardViewModel>(
                missileService
                    .GetAllMissiles()
                    .Select(m => new MissileCardViewModel(m))
            );
        }

        // 메시지 수신 콜백
        public void Receive(MissileUpdateMessage message)
        {
            var update = message.Value;

            // 동일한 ID의 MissileCardViewModel을 찾아서 업데이트
            var vm = Missiles.FirstOrDefault(x => x.Id == update.Id);
            if (vm != null)
            {
                // 위치 변경 반영
                vm.Latitude = update.Latitude;
                vm.Longitude = update.Longitude;
            }
        }
    }
}
