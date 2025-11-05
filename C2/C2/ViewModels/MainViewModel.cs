using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using C2.Services;

namespace C2.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly LogService _logService = LogService.Instance;
        private readonly TargetService _targetService = TargetService.Instance;
        private readonly MissileService _missileService = MissileService.Instance;

        [ObservableProperty] private bool _canAssign = true;
        [ObservableProperty] private bool _canLaunch = false;
        [ObservableProperty] private bool _progressBarVisible = false;
        [ObservableProperty] private double _launchProgress = 0;

        // ✅ 교전할당 버튼 Command
        [RelayCommand]
        private void AssignTarget()
        {


            if(_targetService.SelectedTarget == null)
            {
                _logService.AddLog(Models.MessageType.System, "선택된 표적이 없습니다.");
                CanAssign = false;
                return;
            }

            var targetId = _targetService.SelectedTarget.Id;
            var missileId = _missileService.AssignTarget(targetId.ToString());

            if (missileId == "")
            {
                _logService.AddLog(Models.MessageType.System, "교전할당을 할 수 있는 미사일이 없습니다.");
                CanAssign = false;
                return;
            }

            CanLaunch = true;

            _logService.AddLog(Models.MessageType.System, $"교전할당 완료: {missileId} 생성됨.");
        }

        // ✅ 발사 버튼 Command
        [RelayCommand]
        private async Task LaunchAsync() // 발사 명령 실행하는 부분
        {
            var launchedMissileId = _missileService.StartLaunchMissile();

            // 🚫 발사 가능한 미사일이 없는 경우
            if (launchedMissileId == null)
            {
                CanLaunch = false;
                _logService.AddLog(Models.MessageType.System, "발사 가능한 미사일이 없습니다.");
                return;
            }

            _logService.AddLog(Models.MessageType.System, $"{launchedMissileId} 발사 시작...");

            CanLaunch = false;
            ProgressBarVisible = true;

            for (int i = 1; i <= 100; i++)
            {
                LaunchProgress = i; // 1초마다 20% 증가
                await Task.Delay(50);
            }
            LaunchProgress = 0;
            ProgressBarVisible = false;

            if(_missileService.LaunchMissile() != true)
            {
                _logService.AddLog(Models.MessageType.System, $"{launchedMissileId} 중기유도 실패 ");
                return;
            }

            _logService.AddLog(Models.MessageType.System, $"{launchedMissileId} 발사 완료 ");

            //  발사 조건 확인 후 다시 버튼 활성화

            if (_missileService.AnyRemaining())
                CanLaunch = true;
        }

    }
}
