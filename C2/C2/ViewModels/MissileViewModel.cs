using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2.ViewModels
{
    public partial class MissilePanelViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isCollapsed = false;

        [ObservableProperty]
        private ObservableCollection<Missile> missiles = new();

        private static MissilePanelViewModel _instance;
        public static MissilePanelViewModel Instance => _instance ??= new MissilePanelViewModel();

        private readonly MissileService _service;

        private Missile? _selectedMissile;
        public Missile? SelectedTarget
        {
            get => _selectedMissile;
            set => SetProperty(ref _selectedMissile, value);
        }

        private MissilePanelViewModel()
        {

            //Missiles.Add(new Missile("ISAM-001", 381110000, 1368800000, 0, MissileState.LaunchReady, null));
            //Missiles.Add(new Missile("ISAM-002", 381110000, 1368800000, 0, MissileState.LaunchReady, null));
            //Missiles.Add(new Missile("ISAM-003", 381110000, 1368800000, 0, MissileState.LaunchReady, null));
            //    // 테스트용 더미 데이터
            //Missiles.Add(new Missile("ISAM-004", 381110000, 1368800000, 0, MissileState.MidGuidance, "pyo-001"));
            _service = MissileService.Instance;
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

        public void SelectMissile(Missile missile)
        {
            if (missile == null)
                _service.ClearMissile();
            else
            {
                _service.SelectMissile(missile.Id);
                _selectedMissile = missile;
                //System.Diagnostics.Debug.WriteLine($"[SelectTarget] {target.Id}");
            }
        }
    }
}