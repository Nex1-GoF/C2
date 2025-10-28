using C2.Models;
using C2.Services;
using C2.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace C2.ViewModels
{
    public class MissilePanelViewModel
    {
        public ObservableCollection<MissileCardViewModel> Missiles { get; }

        private readonly MissileService _missileService;
        public MissilePanelViewModel(MissileService missileService)
        {
            _missileService = missileService;

            // MissileService의 Missile 목록을 ViewModel로 감쌈
            Missiles = new ObservableCollection<MissileCardViewModel>(
                missileService
                    .GetAllMissiles()
                    .Select(m => new MissileCardViewModel(m))
            );
        }
    }
}
