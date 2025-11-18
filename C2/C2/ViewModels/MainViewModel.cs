using C2.Messages;
using C2.Models;
using C2.Network;
using C2.Services;
using C2.Network;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Threading.Tasks;
using System.Windows;

namespace C2.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly LogService _logService = LogService.Instance;
        private readonly TargetService _targetService = TargetService.Instance;
        private readonly MissileService _missileService = MissileService.Instance;
        private readonly UpdateDispatcher _updateDispatcher = UpdateDispatcher.Instance;
        private readonly IInitialGuidanceManager _guidanceManager;

        [ObservableProperty] private bool _canAssign = true;
        [ObservableProperty] private bool _progressBarVisible = false;
        [ObservableProperty] private bool _isLaunching = false;
        [ObservableProperty] private bool _canLaunchOrAbort = false;
        [ObservableProperty] private string _currentStep = "";
        [ObservableProperty] private int _currentStepIndex = 0;
        [ObservableProperty] private bool _currentStepIsOn = false;
        private bool _isReversible = false;

        private bool _isCollapsed;
        public bool IsCollapsed
        {
            get => _isCollapsed;
            set
            {
                if (_isCollapsed != value)
                {
                    _isCollapsed = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<string> Steps { get; } = new()
        {
            "전원 점검",
            "BIT 검사",
            "항법 정렬",
            "키 전달",
            "점화 준비",
            "PIP 계산",
            "발사",
        };

        public MainViewModel()
        {
            /*
                실제 -> InitialGuidanceManager
            기능테스트용 -> MockInitialGuidanceManager
             */
            _guidanceManager = InitialGuidanceManager.Instance;
            
            _updateDispatcher.Register(UpdateCanLaunch);

            // ✅ LaunchProgressMessage 수신 → ProgressBar / 상태 동기화
            WeakReferenceMessenger.Default.Register<LaunchProgressMessage>(this, (_, msg) =>
            {
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentStep = msg.StepName;
                    CurrentStepIndex = msg.StepIndex;
                    CurrentStepIsOn = msg.IsOn;
                    IsLaunching = true;
                    if (msg.IsIrreversible && msg.IsOn)
                        CanLaunchOrAbort = false;
                });
            });
            // ✅ 버튼 활성화 메시지 (Launch 절차 완료 후 true로 돌아옴)
            WeakReferenceMessenger.Default.Register<ButtonDeactivateMessage>(this, (_, msg) =>
            {
                _isReversible = msg.Value;
            });
            WeakReferenceMessenger.Default.Register<LaunchEndMessage>(this, (_, msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsLaunching = false;    // UI 감춤
                    CurrentStep = "";       // 초기화
                    CurrentStepIndex = 0;
                    CurrentStepIsOn = false;
                });
                
            });
        }

        private void UpdateCanLaunch()
        {
            CanLaunchOrAbort = _missileService.CanLaunch() || _isReversible;
        }

        [RelayCommand(AllowConcurrentExecutions = true)]
        private async Task LaunchOrAbortAsync()
        {
            if (IsLaunching)
            {
                _guidanceManager.Abort();
                return;
            }

            var missile = _missileService.GetAllMissiles().FirstOrDefault(m => m.State == MissileState.LaunchReady && m.TargetId != null);
            if (missile == null)
            {
                _logService.AddLog(MessageType.System, "발사 가능한 미사일이 없습니다.");
                return;
            }

            IsLaunching = true;
            ProgressBarVisible = true;
            _isReversible = true;

            // ✅ 전체 절차 위임
            await _guidanceManager.StartAsync(missile);
        }

        ~MainViewModel()
        {
            _updateDispatcher.Unregister(UpdateCanLaunch);
            // ✅ 모든 Messenger 구독 해제 (메모리 누수 방지)
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}
