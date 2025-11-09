using C2.Messages;
using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;

namespace C2.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly LogService _logService = LogService.Instance;
        private readonly TargetService _targetService = TargetService.Instance;
        private readonly MissileService _missileService = MissileService.Instance;
        private readonly UpdateDispatcher _updateDispatcher = UpdateDispatcher.Instance;
        private readonly InitialGuidanceManager _guidanceManager = InitialGuidanceManager.Instance;

        [ObservableProperty] private bool _canAssign = true;
        [ObservableProperty] private bool _progressBarVisible = false;
        [ObservableProperty] private double _launchProgress = 0;
        [ObservableProperty] private bool _isLaunching = false;
        [ObservableProperty] private bool _canLaunchOrAbort = false;

        public MainViewModel()
        {
            _updateDispatcher.Register(UpdateCanLaunch);

            // ✅ LaunchProgressMessage 수신 → ProgressBar / 상태 동기화
            WeakReferenceMessenger.Default.Register<LaunchProgressMessage>(this, (_, msg) =>
            {
                LaunchProgress = msg.Progress;
                ProgressBarVisible = msg.Progress < 100;

                if (msg.IsIrreversible)
                    CanLaunchOrAbort = false;
            });

            // ✅ 버튼 활성화 메시지 (Launch 절차 완료 후 true로 돌아옴)
            WeakReferenceMessenger.Default.Register<ButtonDeactivateMessage>(this, (_, msg) =>
            {
                CanLaunchOrAbort = msg.Value;
            });
            WeakReferenceMessenger.Default.Register<LaunchEndMessage>(this, (_, msg) =>
            {
                ProgressBarVisible = false;
                CanLaunchOrAbort = _missileService.AnyRemaining();
                IsLaunching = false;
            });
        }

        private void UpdateCanLaunch()
        {
            CanAssign = _targetService.SelectedTarget != null && _missileService.CanLaunch();
        }

        [RelayCommand]
        private void AssignTarget()
        {
            if (_targetService.SelectedTarget == null)
            {
                _logService.AddLog(MessageType.System, "선택된 표적이 없습니다.");
                CanAssign = false;
                return;
            }

            var targetId = _targetService.SelectedTarget.Id;
            var missileId = _missileService.AssignTarget(targetId.ToString());

            if (string.IsNullOrEmpty(missileId))
            {
                _logService.AddLog(MessageType.System, "교전할당 가능한 미사일이 없습니다.");
                CanAssign = false;
                return;
            }

            _logService.AddLog(MessageType.System, $"교전할당 완료: {missileId} 생성됨.");
            CanLaunchOrAbort = true;

            _targetService.ClearTarget();
            _missileService.ClearMissiles();
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task LaunchOrAbortAsync()
        {
            if (IsLaunching)
            {
                _guidanceManager.Abort();
                return;
            }

            if (!_missileService.CanLaunch())
            {
                _logService.AddLog(MessageType.System, "발사 가능한 미사일이 없습니다.");
                return;
            }

            IsLaunching = true;
            ProgressBarVisible = true;

            // ✅ 전체 절차 위임
            await _guidanceManager.StartAsync();

            // ❌ 아래 로직은 GuidanceManager가 완료 시점에 메시지로 처리하므로 제거
            // IsLaunching = false;
            // ProgressBarVisible = false;
            // CanLaunchOrAbort = _missileService.AnyRemaining();
        }

        ~MainViewModel()
        {
            _updateDispatcher.Unregister(UpdateCanLaunch);
            // ✅ 모든 Messenger 구독 해제 (메모리 누수 방지)
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}
