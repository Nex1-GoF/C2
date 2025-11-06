using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading;
using System.Threading.Tasks;

namespace C2.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly LogService _logService = LogService.Instance;
        private readonly TargetService _targetService = TargetService.Instance;
        private readonly MissileService _missileService = MissileService.Instance;
        private readonly UpdateDispatcher _updateDispatcher = UpdateDispatcher.Instance;

        [ObservableProperty] private bool _canAssign = true;
        [ObservableProperty] private bool _progressBarVisible = false;
        [ObservableProperty] private double _launchProgress = 0;
        [ObservableProperty] private bool _isLaunching = false;      // 발사 중 여부
        [ObservableProperty] private bool _canLaunchOrAbort = false;   // 발사 | 발사취소 버튼 활성 여부

        private CancellationTokenSource? _launchCts; // 취소 토큰

        public MainViewModel()
        {
            _updateDispatcher.Register(UpdateCanLaunch);
        }

        private void UpdateCanLaunch()
        {
            if (_targetService.SelectedTarget == null || !_missileService.CanLaunch())
            {
                CanAssign = false;
                return;
            }
            CanAssign = true;
        }

        // ✅ 교전할당
        [RelayCommand]
        private void AssignTarget()
        {
            if (_targetService.SelectedTarget == null)
            {
                _logService.AddLog(Models.MessageType.System, "선택된 표적이 없습니다.");
                CanAssign = false;
                return;
            }

            var targetId = _targetService.SelectedTarget.Id;
            var missileId = _missileService.AssignTarget(targetId.ToString());

            if (string.IsNullOrEmpty(missileId))
            {
                _logService.AddLog(Models.MessageType.System, "교전할당 가능한 미사일이 없습니다.");
                CanAssign = false;
                return;
            }

            CanLaunchOrAbort = true;
            _logService.AddLog(Models.MessageType.System, $"교전할당 완료: {missileId} 생성됨.");

            _targetService.ClearTarget();
            _missileService.ClearMissiles();
        }
        [RelayCommand]
        private async Task LaunchOrAbortAsync()
        {
            if (IsLaunching)
            {
                AbortLaunch();
            }
            else
            {
                await LaunchAsync();
            }
        }
        // ✅ 발사 로직
        private async Task LaunchAsync()
        {
            if (IsLaunching)
            {
                _logService.AddLog(Models.MessageType.System, "이미 발사 중입니다.");
                return;
            }

            var launchedMissileId = _missileService.StartLaunchMissile();
            if (launchedMissileId == null)
            {
                CanLaunchOrAbort = false;
                _logService.AddLog(Models.MessageType.System, "발사 가능한 미사일이 없습니다.");
                return;
            }

            _logService.AddLog(Models.MessageType.System, $"{launchedMissileId} 발사 시작...");

            _launchCts = new CancellationTokenSource();
            var token = _launchCts.Token;

            IsLaunching = true;
            ProgressBarVisible = true;

            try
            {
                // 0~50% 구간 (취소 가능)
                for (int i = 1; i <= 50; i++)
                {
                    if (token.IsCancellationRequested)
                        throw new TaskCanceledException();

                    LaunchProgress = i;
                    await Task.Delay(50, token);
                }

                // 중기 유도 진입 → 취소 불가
                CanLaunchOrAbort = false;

                for (int i = 51; i <= 100; i++)
                {
                    LaunchProgress = i;
                    await Task.Delay(50);
                }

                LaunchProgress = 0;
                ProgressBarVisible = false;

                if (!_missileService.LaunchMissile())
                {
                    _logService.AddLog(Models.MessageType.System, $"{launchedMissileId} 중기유도 실패 ");
                    return;
                }

                _logService.AddLog(Models.MessageType.System, $"{launchedMissileId} 발사 완료");

                // ✅ PIP 생성
                var missile = _missileService.GetMissile(launchedMissileId);
                if (missile != null && missile.TargetId != null)
                {
                    var target = _targetService.GetTarget(missile.TargetId[0]);
                    if (target != null)
                    {
                        double pipLat = (missile.Latitude + target.CurLoc.Lat) / 2.0;
                        double pipLon = (missile.Longitude + target.CurLoc.Lon) / 2.0;
                        missile.PIP = new PIP(pipLat, pipLon, 0);

                        _logService.AddLog(Models.MessageType.System,
                            $"{launchedMissileId}의 PIP 생성 완료 ({pipLat:F4}, {pipLon:F4})");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                _logService.AddLog(Models.MessageType.System, "🚫 발사 취소됨.");
                LaunchProgress = 0;
                ProgressBarVisible = false;
            }
            finally
            {
                IsLaunching = false;
                _launchCts = null;
                CanLaunchOrAbort = _missileService.AnyRemaining();
            }
        }

        // ✅ 발사취소

        private void AbortLaunch()
        {
            if (!IsLaunching || _launchCts == null)
            {
                _logService.AddLog(Models.MessageType.System, "취소할 발사 진행이 없습니다.");
                return;
            }

            _launchCts.Cancel();
            _logService.AddLog(Models.MessageType.System, "발사 취소 명령 전송됨.");
        }

        ~MainViewModel()
        {
            _updateDispatcher.Unregister(UpdateCanLaunch);
        }
    }
}
