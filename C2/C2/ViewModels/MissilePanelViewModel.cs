using C2.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using C2.Models;

namespace C2.ViewModels
{
    public class MissilePanelViewModel
    {
        public ObservableCollection<MissileCardViewModel> Missiles { get; }

        public MissilePanelViewModel()
        {
            // 뷰에 표현할 데이터 생성 (테스트데이터)
            Missiles = new ObservableCollection<MissileCardViewModel>
            {
                new(new Missile("MSL-001", 37.56, 126.97, 250, MissileState.Standby, null)),
                new(new Missile("MSL-002", 35.17, 129.07, 300, MissileState.MidGuidance, "TGT-01")),
                new(new Missile("MSL-003", 36.33, 127.43, 280, MissileState.TerminalGuidance, null))
            };
        }
    }
}
