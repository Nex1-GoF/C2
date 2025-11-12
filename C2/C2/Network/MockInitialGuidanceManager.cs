using C2.Messages;
using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace C2.Network
{
    public class MockInitialGuidanceManager: IInitialGuidanceManager
    {
        private static MockInitialGuidanceManager _instance;
        public static MockInitialGuidanceManager Instance => _instance ??= new MockInitialGuidanceManager();

        private readonly LogService _logService = LogService.Instance;
        private readonly MissileService _missileService = MissileService.Instance;
        private readonly TargetService _targetService = TargetService.Instance;

        private IGuidanceState? _currentState;
        private CancellationTokenSource? _cts;
        private double _currentProgress = 0;

        private MockInitialGuidanceManager() { }

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

        // ✅ 실제 InitialGuidanceManager처럼 미사일을 인자로 받도록 수정
        public async Task StartAsync(Missile missile)
        {
            if (_cts != null)
            {
                _logService.AddLog(MessageType.System, "이미 발사 절차가 진행 중입니다.");
                return;
            }

            _cts = new CancellationTokenSource();
            _currentProgress = 0;
            _logService.AddLog(MessageType.System, $"[{missile.Id}] 발사 절차 시작됨");

            _currentState = new PowerOnState(this, missile);
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
                WeakReferenceMessenger.Default.Send(new LaunchEndMessage(true));
                _cts = null;
                _currentState = null;
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
        // 📍 State Pattern 내부 클래스
        // =====================================================
        internal interface IGuidanceState
        {
            Task EnterAsync(CancellationToken token);
            IGuidanceState? NextState { get; }
        }

        internal abstract class BaseGuidanceState : IGuidanceState
        {
            protected readonly MockInitialGuidanceManager _manager;
            protected readonly Missile _missile;

            public abstract string Name { get; }
            public abstract IGuidanceState? NextState { get; }

            protected BaseGuidanceState(MockInitialGuidanceManager manager, Missile missile)
            {
                _manager = manager;
                _missile = missile;
            }

            public virtual async Task EnterAsync(CancellationToken token)
            {
                _manager._logService.AddLog(MessageType.System, $"{Name} 수행 중...");
                double targetProgress = _manager._phaseProgressMap[GetType()];
                await _manager.SmoothProgressToAsync(targetProgress, 1500, token);
            }
        }

        private class PowerOnState : BaseGuidanceState
        {
            public override string Name => "전원 점검";
            public override IGuidanceState NextState => new BitCheckState(_manager, _missile);
            public PowerOnState(MockInitialGuidanceManager manager, Missile missile) : base(manager, missile) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                var missileId = _manager._missileService.UpdateMissileState(MissileState.LaunchReady, MissileState.Launching);
                if (missileId != null)
                    _manager._logService.AddLog(MessageType.System, $"{missileId} 발사 단계로 전환됨");

                await base.EnterAsync(token);
                await Task.Delay(10, token);
            }
        }

        private class BitCheckState : BaseGuidanceState
        {
            public override string Name => "BIT 검사";
            public override IGuidanceState NextState => new AlignState(_manager, _missile);
            public BitCheckState(MockInitialGuidanceManager manager, Missile missile) : base(manager, missile) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);
                await Task.Delay(10, token);
            }
        }

        private class AlignState : BaseGuidanceState
        {
            public override string Name => "항법 정렬";
            public override IGuidanceState NextState => new KeyState(_manager, _missile);
            public AlignState(MockInitialGuidanceManager manager, Missile missile) : base(manager, missile) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);
                await Task.Delay(10, token);
            }
        }

        private class KeyState : BaseGuidanceState
        {
            public override string Name => "키 전달";
            public override IGuidanceState NextState => new IgnitionState(_manager, _missile);
            public KeyState(MockInitialGuidanceManager manager, Missile missile) : base(manager, missile) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);
                byte[] key = GenerateSessionKey();
                _manager._logService.AddLog(MessageType.System, $"세션 키 생성 완료 ({key.Length} bytes)");
                await Task.Delay(10, token);
            }
        }

        private class IgnitionState : BaseGuidanceState
        {
            public override string Name => "점화 준비";
            public override IGuidanceState NextState => new PipCalculationState(_manager, _missile);
            public IgnitionState(MockInitialGuidanceManager manager, Missile missile) : base(manager, missile) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);
                await Task.Delay(10, token);
            }
        }

        private class PipCalculationState : BaseGuidanceState
        {
            public override string Name => "PIP 계산";
            public override IGuidanceState NextState => new LaunchState(_manager, _missile);
            public PipCalculationState(MockInitialGuidanceManager manager, Missile missile) : base(manager, missile) { }
            private static (double x, double y) LatLonToXY(double lat, double lon, double lat0, double lon0)
            {
                const double R = 6_378_137.0; // 지구 반경 (m)
                double lat0Rad = lat0 * Math.PI / 180.0;
                double dLat = (lat - lat0) * Math.PI / 180.0;
                double dLon = (lon - lon0) * Math.PI / 180.0;

                double x = dLon * R * Math.Cos(lat0Rad);
                double y = dLat * R;
                return (x, y);
            }
            private (double lat, double lon) XYToLatLon(double x, double y, double lat0, double lon0)
            {
                const double R = 6_378_137.0;
                double lat0Rad = lat0 * Math.PI / 180.0;

                double newLat = lat0 + (y / R) * (180.0 / Math.PI);
                double newLon = lon0 + (x / (R * Math.Cos(lat0Rad))) * (180.0 / Math.PI);

                return (newLat, newLon);
            }

            public static (int pipX, int pipY) ComputePIP(
            Target target,
            double missileLat, double missileLon,
            int missileSpeed,            // ✅ int 단위 (m/s)
            double lat0, double lon0)
            {
                // 1️⃣ 위경도 → 평면 좌표
                var (tx, ty) = LatLonToXY(target.CurLoc.Lat, target.CurLoc.Lon, lat0, lon0);
                var (mx, my) = LatLonToXY(missileLat, missileLon, lat0, lon0);

                // 2️⃣ Yaw (1e7으로 스케일 보정)
                //double yawDeg = target.Yaw / 1e7;
                double yawDeg = 175.0;
                double theta = yawDeg * Math.PI / 180.0;

                // 3️⃣ 타겟 진행 방향 단위벡터 * 속도(m/s)
                double dx = target.Speed * Math.Sin(theta);
                double dy = target.Speed * Math.Cos(theta);

                // 4️⃣ 교차 시간 t 탐색 (선형 탐색)
                double t = 0.0;
                double left = 0, right = 2000; // 최대 1000초 탐색
                for (int i = 0; i < 100; i++)
                {
                    t = (left + right) / 2.0;
                    double tx_t = tx + dx * t;
                    double ty_t = ty + dy * t;
                    double dist = Math.Sqrt(Math.Pow(tx_t - mx, 2) + Math.Pow(ty_t - my, 2));

                    if (dist > missileSpeed * t)
                        left = t;
                    else
                        right = t;
                }

                // 5️⃣ 최종 PIP 계산 (1e7 스케일 적용 후 정수 변환)
                int pipX = (int)Math.Round((tx + dx * t));
                int pipY = (int)Math.Round((ty + dy * t));

                return (pipX, pipY);
            }
            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);

                // Todo: X Y Z 변환해서 보내야함
                char targetId = _missile.TargetId[0];
                // 1. PIP를 구한다. (현재 표적 위 경 고도
                var target = _manager._targetService.GetTarget(targetId);

                if (target == null)
                {
                    //NextState = null;
                    return;
                }

                (int x, int y) targetXY = ComputePIP(
                     target: target,
                     missileLat: _missile.Latitude,
                     missileLon: _missile.Longitude,
                     missileSpeed: 6000,
                     lat0: _missile.Latitude,
                     lon0: _missile.Longitude

                );

               // byte[] pip = BuildPipBody(targetXY.x, targetXY.y, 10);

                (double lat, double lon) targetLatLon = XYToLatLon(targetXY.x, targetXY.y, _missile.Latitude, _missile.Longitude);

                _missile.PIP = new PIP(targetLatLon.lat, targetLatLon.lon, 10);
                WeakReferenceMessenger.Default.Send(new PipCalculatedMessage(_missile.Id, targetLatLon.lat, targetLatLon.lon, 10));

                await Task.Delay(10, token);
            }
        }

        private class LaunchState : BaseGuidanceState
        {
            public override string Name => "발사";
            public override IGuidanceState? NextState => new InitialGuidanceState(_manager, _missile);
            public LaunchState(MockInitialGuidanceManager manager, Missile missile) : base(manager, missile) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                var missileId = _manager._missileService.UpdateMissileState(MissileState.Launching, MissileState.InitialGuidance);
                if (missileId != null)
                    _manager._logService.AddLog(MessageType.System, $"{missileId} 초기유도 단계로 전환됨");

                await base.EnterAsync(token);
                await Task.Delay(500, token);
            }
        }

        private class InitialGuidanceState : BaseGuidanceState
        {
            public override string Name => "초기유도";
            public override IGuidanceState? NextState => null;
            public InitialGuidanceState(MockInitialGuidanceManager manager, Missile missile) : base(manager, missile) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                var missileId = _manager._missileService.UpdateMissileState(MissileState.InitialGuidance, MissileState.MidGuidance);
                if (missileId != null)
                    _manager._logService.AddLog(MessageType.System, $"{missileId} 중기유도 단계로 전환됨");

                await base.EnterAsync(token);
                await Task.Delay(5000, token);
            }
        }
    }
}
