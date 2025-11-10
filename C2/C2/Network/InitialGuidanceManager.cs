using C2.Messages;
using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace C2.Network
{
    public class InitialGuidanceManager
    {
        private static InitialGuidanceManager _instance;
        public static InitialGuidanceManager Instance => _instance ??= new InitialGuidanceManager();

        private readonly LogService _logService = LogService.Instance;
        private readonly MissileService _missileService = MissileService.Instance;
        private readonly TargetService _targetService = TargetService.Instance;

        private IGuidanceState? _currentState;
        private CancellationTokenSource? _cts;
        private double _currentProgress = 0;

        private InitialGuidanceManager() { }

        private readonly Dictionary<Type, double> _phaseProgressMap = new()
        {
            { typeof(PowerOnState), 17 },
            { typeof(BitCheckState), 33 },
            { typeof(AlignState), 41 },
            { typeof(KeyState), 50 },
            { typeof(IgnitionState), 67 },
            { typeof(PipCalculationState), 83 },
            { typeof(LaunchState), 100 },
            { typeof(InitialGuidanceState), 100 },
        };

        public async Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _currentProgress = 0;

            _currentState = new PowerOnState(this);
            await RunStateMachineAsync(_cts.Token);
        }

        public void Abort()
        {
            _cts?.Cancel();
            _logService.AddLog(MessageType.System, "발사 절차 중단됨");
        }


         private async Task RunStateMachineAsync(CancellationToken token)
        {
            try
            {
                while (_currentState != null)
                {
                    await _currentState.EnterAsync(token);

                    if (_currentState is KeyState)
                    {
                        _logService.AddLog(MessageType.System, "비가역 상태 진입");
                        WeakReferenceMessenger.Default.Send(
                            new LaunchProgressMessage(_currentProgress, isIrreversible: true));
                        WeakReferenceMessenger.Default.Send(new ButtonDeactivateMessage(false));
                    }

                    if (_currentState is LaunchState)
                    {
                        _logService.AddLog(MessageType.System, "발사 절차 완료");

                        var launchedId = _missileService.UpdateMissileState(
                            MissileState.InitialGuidance, MissileState.MidGuidance);
                        if (launchedId != null)
                            _logService.AddLog(MessageType.System, $"{launchedId} 중기유도 단계로 전환됨");

                        WeakReferenceMessenger.Default.Send(new LaunchProgressMessage(100));
                    }

                    _currentState = _currentState.NextState;
                }
            }
            catch (TaskCanceledException)
            {
                _missileService.CancelLaunch();
                _logService.AddLog(MessageType.System, "절차가 사용자에 의해 중단됨");
            }
            catch (Exception ex)
            {
                _missileService.CancelLaunch();
                _logService.AddLog(MessageType.System, $"오류 발생: {ex.Message}");
            }
            finally
            {
                // ✅ 절차가 끝나거나 중단되었을 때 무조건 UI 복귀
                WeakReferenceMessenger.Default.Send(new LaunchEndMessage(true));
            }
        }


        internal async Task SmoothProgressToAsync(double target, int durationMs, CancellationToken token)
        {
            const int stepTime = 50;
            int steps = durationMs / stepTime;
            double start = _currentProgress;

            for (int i = 0; i <= steps; i++)
            {
                if (token.IsCancellationRequested)
                    throw new TaskCanceledException();

                _currentProgress = start + (target - start) * i / steps;

                // ✅ 프로그레스만 갱신 (로그는 여기서 하지 않음)
                WeakReferenceMessenger.Default.Send(new LaunchProgressMessage(_currentProgress));

                await Task.Delay(stepTime, token);
            }
        }

        internal static byte[] GenerateSessionKey()
        {
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            return key;
        }

        // =====================================================
        // 📍 State Pattern 내부 클래스들
        // =====================================================

        internal interface IGuidanceState
        {
            Task EnterAsync(CancellationToken token);
            IGuidanceState? NextState { get; }
        }

        internal abstract class BaseGuidanceState : IGuidanceState
        {
            protected readonly InitialGuidanceManager _manager;
            public abstract string Name { get; }
            public abstract IGuidanceState? NextState { get; }
            protected BaseGuidanceState(InitialGuidanceManager manager) => _manager = manager;

            public virtual async Task EnterAsync(CancellationToken token)
            {
                // ✅ 로그는 이 시점에서 출력
                _manager._logService.AddLog(MessageType.System, $"{Name} 수행 중...");

                double targetProgress = _manager._phaseProgressMap[GetType()];
                await _manager.SmoothProgressToAsync(targetProgress, 1500, token);
            }
        }


        private class PowerOnState : BaseGuidanceState
        {
            public override string Name => "전원 점검";
            public override IGuidanceState NextState => new BitCheckState(_manager);
            public PowerOnState(InitialGuidanceManager manager) : base(manager) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                var missileId = _manager._missileService.UpdateMissileState(MissileState.LaunchReady, MissileState.Launching); // 발사준비완료 -> 발사시작
                if (missileId != null)
                {
                    _manager._logService.AddLog(MessageType.System, $"{missileId} 발사 단계로 전환됨");
                }

                await base.EnterAsync(token);

                // TODO: 실제 전원 점검 로직
                //TODO: NetworkMessages에서 Msg_II0010 클래스 이용

                await Task.Delay(500, token); // TODO: 폴링으로 바꿔야함 마지막에
                        // TODO: BIT 메세지 송신
            }
        }

        private class BitCheckState : BaseGuidanceState
        {
            public override string Name => "BIT 검사";
            public override IGuidanceState NextState => new AlignState(_manager);
            public BitCheckState(InitialGuidanceManager manager) : base(manager) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);

                        // TODO: BIT 메세지 송신
                        //TODO: NetworkMessages에서 Msg_II0010 클래스 이용

                await Task.Delay(500, token); // TODO: 폴링으로 바꿔야함 마지막에
            }
        }

        private class AlignState : BaseGuidanceState
        {
            public override string Name => "항법 정렬";
            public override IGuidanceState NextState => new KeyState(_manager);
            public AlignState(InitialGuidanceManager manager) : base(manager) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);

                byte[] key = GenerateSessionKey();

                // --------------------------------------------------------------------------------------

                // TODO: 항법장치 메세지 송신  
                //TODO: NetworkMessages에서 Msg_II0010 클래스 이용 (항법장치 정렬에 필요한 인자가 없고, 세션 키 전달이랑 같이 할 경우)

                await Task.Delay(500, token); // TODO: 폴링으로 바꿔야함 마지막에
            }
        }
        private class KeyState : BaseGuidanceState
        {
            public override string Name => "키 전달";
            public override IGuidanceState NextState => new IgnitionState(_manager);
            public KeyState(InitialGuidanceManager manager) : base(manager) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);

                byte[] key = GenerateSessionKey();


                        // TODO: 세션키 메세지 송신  
                        //TODO: NetworkMessages에서 Msg_II0012 클래스 이용 (항법장치 정렬에 필요한 인자가 없고, 세션 키 전달이랑 같이 할 경우)

                await Task.Delay(500, token); // TODO: 폴링으로 바꿔야함 마지막에
            }
        }

        private class IgnitionState : BaseGuidanceState
        {
            public override string Name => "점화 준비";
            public override IGuidanceState NextState => new PipCalculationState(_manager);
            public IgnitionState(InitialGuidanceManager manager) : base(manager) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);
                        // TODO: 점화 신호 준비 로직
                        //TODO: NetworkMessages에서 Msg_II0010 클래스 이용

                await Task.Delay(500, token); // TODO: 폴링으로 바꿔야함 마지막에
            }
        }

        private class PipCalculationState : BaseGuidanceState
        {
            public override string Name => "PIP 계산";
            public override IGuidanceState NextState => new LaunchState(_manager);
            public PipCalculationState(InitialGuidanceManager manager) : base(manager) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);

                // ✅ 시뮬레이션용: 네트워크 송신 없음
                var missile = _manager._missileService.GetAllMissiles().FirstOrDefault(m=>m.State==MissileState.Launching);
                if (missile == null)
                {
                    return;
                }

                var targetId = missile.TargetId;
                var target = _manager._targetService.GetTarget(targetId[0]);
                if (target == null)
                {
                    return;
                }

                // 미사일 / 표적 좌표
                double mLat = missile.Latitude;
                double mLon = missile.Longitude;
                double tLat = target.CurLoc.Lat;
                double tLon = target.CurLoc.Lon;

                // 중간 지점 계산
                double pipLat = (mLat + tLat) / 2.0;
                double pipLon = (mLon + tLon) / 2.0;

                // 미사일 객체에 저장 (간단히)
                missile.PIP = new PIP(pipLat, pipLon, 0);

                _manager._logService.AddLog(
                    MessageType.System,
                    $"PIP 계산 완료: ({pipLat:F6}, {pipLon:F6}) 추가됨"
                );

                await Task.Delay(500, token); // 시뮬레이션용 딜레이
            }
        }


        private class LaunchState : BaseGuidanceState
        {
            public override string Name => "발사";
            public override IGuidanceState? NextState => new InitialGuidanceState(_manager);
            public LaunchState(InitialGuidanceManager manager) : base(manager) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                var missileId = _manager._missileService.UpdateMissileState(MissileState.Launching, MissileState.InitialGuidance); // 발사중 -> 초기유도로 전환
                if (missileId != null)
                {
                    _manager._logService.AddLog(MessageType.System, $"{missileId} 초기유도 단계로 전환됨");
                }

                await base.EnterAsync(token);
                        // TODO: 발사 신호 준비 로직
                        //TODO: NetworkMessages에서 Msg_II0010 클래스 이용

                await Task.Delay(500, token); // TODO: 폴링으로 바꿔야함 마지막에
            }
        }
        private class InitialGuidanceState : BaseGuidanceState
        {
            public override string Name => "초기유도";
            public override IGuidanceState? NextState => null;
            public InitialGuidanceState(InitialGuidanceManager manager) : base(manager) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                var missileId = _manager._missileService.UpdateMissileState(MissileState.InitialGuidance, MissileState.MidGuidance); // 발사중 -> 초기유도로 전환
                if (missileId != null)
                {
                    _manager._logService.AddLog(MessageType.System, $"{missileId} 중기유도 단계로 전환됨");
                }
                await base.EnterAsync(token);
                        // TODO: 발사 신호 준비 로직
                        //TODO: NetworkMessages에서 Msg_II0010 클래스 이용

                await Task.Delay(5000, token); // TODO: 폴링으로 바꿔야함 마지막에
            }
        }
    }
}
