using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using C2.Services;

namespace C2.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly LogService _logService = LogService.Instance;

        [ObservableProperty] private bool _canAssign = true;
        [ObservableProperty] private bool _canLaunch = false;
        [ObservableProperty] private bool _progressBarVisible = false;
        [ObservableProperty] private double _launchProgress = 0;

        // ✅ 교전할당 버튼 Command
        [RelayCommand]
        private void AssignTarget()
        {
            var success = MockMissileService.Instance.TryAssignMissile();

            if (!success)
            {
                CanAssign = false;
                _logService.AddLog(Models.MessageType.System, "최대 4기 미사일이 모두 교전할당되었습니다.");
                return;
            }

            // ✅ 첫 번째 미사일 생성 시 발사 버튼 활성화
            CanLaunch = true;

            int count = MockMissileService.Instance.missileControllers.Count;
            _logService.AddLog(Models.MessageType.System, $"교전할당 완료: MSL-{count:00} 생성됨.");

            // ✅ 4개 도달 시 비활성화
            if (count >= 4)
            {
                CanAssign = false;
                _logService.AddLog(Models.MessageType.System, "최대 4기 미사일이 모두 교전할당되었습니다.");
            }
        }

        // ✅ 발사 버튼 Command
        [RelayCommand]
        private async Task LaunchAsync() // 발사 명령 실행하는 부분
        {
            var success = MockMissileService.Instance.TryLaunchNextMissile();

            // 🚫 발사 가능한 미사일이 없는 경우
            if (!success)
            {
                CanLaunch = false;
                _logService.AddLog(Models.MessageType.System, "모든 미사일이 이미 발사되었습니다.");
                return;
            }

            int launched = MockMissileService.Instance.missileControllers.FindAll(m => m.IsLaunched).Count;
            _logService.AddLog(Models.MessageType.System, $"MSL-{launched:00} 발사 시작...");

            CanLaunch = false;
            ProgressBarVisible = true;

            for (int i = 1; i <= 100; i++)
            {
                LaunchProgress = i; // 1초마다 20% 증가
                await Task.Delay(50);
            }
            LaunchProgress = 0;
            ProgressBarVisible = false;

            _logService.AddLog(Models.MessageType.System, $"MSL-{launched:00} 발사 완료 ");

            //  발사 조건 확인 후 다시 버튼 활성화
            bool anyRemaining = MockMissileService.Instance.missileControllers.Exists(m => !m.IsLaunched);
            if (anyRemaining)
                CanLaunch = true;

            //  방금 발사된 미사일 수 확인
            int total = MockMissileService.Instance.missileControllers.Count;


            //  모든 미사일이 발사 완료되면 버튼 비활성화
            if (launched >= total)
            {
                CanLaunch = false;
                _logService.AddLog(Models.MessageType.System, "모든 미사일이 이미 발사되었습니다.");
            }
        }

    }
}
