using C2.Messages;
using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        private MissileService _missileService = MissileService.Instance;
        private readonly TargetService _targetService = TargetService.Instance;

        private IGuidanceState? _currentState;
        private CancellationTokenSource? _cts;
        private double _currentProgress = 0;

        private MockInitialGuidanceManager() { }



        private readonly List<string> Steps = new()
        {
            "전원 점검",
            "BIT 검사",
            "항법 정렬",
            "키 전달",
            "점화 준비",
            "PIP 계산",
            "발사",
            "초기유도"
        };
        private int GetStepIndex(string name)
        {
            return Steps.IndexOf(name)+1;  // 1-based
        }

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
            await RunStateMachineAsync(_cts.Token, missile);
        }

        public void Abort()
        {
            _cts?.Cancel();
            _logService.AddLog(MessageType.System, "발사 절차 중단됨");
        }

        private async Task RunStateMachineAsync(CancellationToken token, Missile missile)
        {
            bool flag = true;
            try
            {
                WeakReferenceMessenger.Default.Send(new MissileLaunchMessage(missile.Id));
                while (_currentState != null)
                {
                    await _currentState.EnterAsync(token);
                    _currentState = _currentState.NextState;
                }
            }
            catch (TaskCanceledException)
            {
                _missileService.CancelLaunch();
                _logService.AddLog(MessageType.System, "절차가 사용자에 의해 중단됨");
                flag = false;
            }
            catch (Exception ex)
            {
                _missileService.CancelLaunch();
                _logService.AddLog(MessageType.System, $"오류 발생: {ex.Message}");
                flag = false;
            }
            finally
            {
                WeakReferenceMessenger.Default.Send(new LaunchEndMessage(missile.Id));
                _cts = null;
                _currentState = null;
                if(flag == true) await startMidGuid(token, missile);
            }
        }

        private async Task startMidGuid(CancellationToken token, Missile missile)
        {
            var missileId = _missileService.UpdateMissileState(
                MissileState.InitialGuidance, MissileState.MidGuidance);

            await Task.Delay(500, token);

            double speed = 500.0;  // m/s
            double dt = 0.05;      // 50ms
            double metersPerDegree = 111000.0;

            int totalSteps = (int)(300.0 / dt);

            // YawRaw(단위: deg × 100) → rad 변환
            double yawDeg = missile.YawRaw / 100.0;
            double yawRad = yawDeg * Math.PI / 180.0;

            // 방향 벡터 (동쪽,북쪽)
            double vx = speed * Math.Sin(yawRad);   // east velocity (m/s)
            double vy = speed * Math.Cos(yawRad);   // north velocity (m/s)

            for (int i = 0; i < totalSteps; i++)
            {
                if (token.IsCancellationRequested) break;
                if (missile.State == MissileState.Abort) break;

                // 1) 현재 위치
                double lat = missile.Latitude;
                double lon = missile.Longitude;

                // 2) 거리(m → deg 단위 변환)
                double dLat = (vy * dt) / metersPerDegree;
                double dLon = (vx * dt) / (metersPerDegree * Math.Cos(lat * Math.PI / 180.0));

                // 3) 업데이트
                double newLat = lat + dLat;
                double newLon = lon + dLon;

                missile.LatitudeRaw = (int)(newLat * 1e7);
                missile.LongitudeRaw = (int)(newLon * 1e7);

                await Task.Delay((int)(dt * 1000), token);
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
            Task ExitAsync(CancellationToken token);
            IGuidanceState? NextState { get; }
        }

        internal abstract class BaseGuidanceState : IGuidanceState
        {
            protected readonly MockInitialGuidanceManager _manager;
            protected Missile _missile;

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
                int idx = _manager.GetStepIndex(Name);

                if (this is IgnitionState)
                {
                    _manager._logService.AddLog(MessageType.System, "비가역 상태 진입");
                    WeakReferenceMessenger.Default.Send(
                        new LaunchProgressMessage(Name, idx, false, isIrreversible: true));
                    WeakReferenceMessenger.Default.Send(new ButtonDeactivateMessage(false));
                }
                else if (this is LaunchState)
                {
                    _manager._logService.AddLog(MessageType.System, "발사 절차 완료");

                    var launchedId = _manager._missileService.UpdateMissileState(
                        MissileState.InitialGuidance, MissileState.MidGuidance);
                    if (launchedId != null)
                        _manager._logService.AddLog(MessageType.System, $"{launchedId} 중기유도 단계로 전환됨");

                    WeakReferenceMessenger.Default.Send(new LaunchProgressMessage(Name, idx, false));
                }
                else
                    WeakReferenceMessenger.Default.Send(new LaunchProgressMessage(Name, idx, false));

            }

            public virtual async Task ExitAsync(CancellationToken token)
            {
                int idx = _manager.GetStepIndex(Name);
                WeakReferenceMessenger.Default.Send(new LaunchProgressMessage(Name, idx, true));
                _manager._logService.AddLog(MessageType.System, $"{Name} 완료");
                await Task.Delay(500, token);
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
                await Task.Delay(1000, token);
                await base.ExitAsync(token);

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
                await Task.Delay(1000, token);
                await base.ExitAsync(token);
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
                await Task.Delay(1000, token);
                await base.ExitAsync(token);
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
                await Task.Delay(1000, token);
                await base.ExitAsync(token);
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
                await Task.Delay(1000, token);
                await base.ExitAsync(token);
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
            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);

                var targetId = _missile.TargetId?.FirstOrDefault();
                if (targetId == null)
                {
                    return;
                }

                var target = _manager._targetService.GetTarget(targetId ?? '\0');
                if (target == null)
                {
                    return;
                }

                double mLat = _missile.Latitude;
                double mLon = _missile.Longitude;
                double tLat = target.CurLoc.Lat;
                double tLon = target.CurLoc.Lon;

                double pipLat = (mLat + tLat) / 2.0;
                double pipLon = (mLon + tLon) / 2.0;

                _missile.PIP = new PIP(pipLat, pipLon, 0);

                var (mx, my) = LatLonToXY(mLat, mLon, mLat, mLon);
                var (px, py) = LatLonToXY(pipLat, pipLon, mLat, mLon);

                double dx = px - mx;
                double dy = py - my;

                double rawYaw = Math.Atan2(dx, dy) * (180.0 / Math.PI);

                double yawDeg = rawYaw;
                if (yawDeg < 0) yawDeg += 360.0;

                int yawRaw = (int)(yawDeg * 100);
                _missile.YawRaw = yawRaw;

                _manager._logService.AddLog(
                    MessageType.System,
                    $"[{_missile.Id}] PIP 계산 완료: ({pipLat:F6}, {pipLon:F6})"
                );

                await Task.Delay(1000, token);
                await base.ExitAsync(token);
            }
        }

        private class LaunchState : BaseGuidanceState
        {
            public override string Name => "발사";
            public override IGuidanceState? NextState => null;
            public LaunchState(MockInitialGuidanceManager manager, Missile missile) : base(manager, missile) { }

            public override async Task EnterAsync(CancellationToken token)
            {
                var missileId = _manager._missileService.UpdateMissileState(MissileState.Launching, MissileState.InitialGuidance);
                if (missileId != null)
                    _manager._logService.AddLog(MessageType.System, $"{missileId} 초기유도 단계로 전환됨");

                await base.EnterAsync(token);
                await Task.Delay(1000, token);

                await base.ExitAsync(token);
                
            }
        }

    }
}
