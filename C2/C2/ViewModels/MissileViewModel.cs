using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C2.Models;

namespace C2.ViewModels
{
    public partial class MissilePanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isCollapsed = false;

        [ObservableProperty]
        private ObservableCollection<Missile> missiles = new();

        public MissilePanelViewModel()
        {
            
            Missiles.Add(new Missile("ISAM-001", 381110000, 1368800000, 0, MissileState.LaunchReady, null));
            Missiles.Add(new Missile("ISAM-002", 381110000, 1368800000, 0, MissileState.LaunchReady, null));
            Missiles.Add(new Missile("ISAM-003", 381110000, 1368800000, 0, MissileState.LaunchReady, null));
                // 테스트용 더미 데이터
            Missiles.Add(new Missile("ISAM-004", 381110000, 1368800000, 0, MissileState.MidGuidance, "pyo-001"));
        }

        [RelayCommand]
        private void ToggleCollapse()
        {
            IsCollapsed = !IsCollapsed;
        }

        [RelayCommand]
        private void Launch(Missile missile)
        {
            missile.State = MissileState.InitialGuidance;
        }

        [RelayCommand]
        private void Abort(Missile missile)
        {
            missile.State = MissileState.Abort;
        }
    }
}