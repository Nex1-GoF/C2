using C2.Config;
using C2.Messages;
using C2.Models;
using C2.Services;
using CommunityToolkit.Mvvm.Messaging;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace C2.Network
{
    public class InitialGuidanceManager : IInitialGuidanceManager
    {
        private static InitialGuidanceManager _instance;
        public static InitialGuidanceManager Instance => _instance ??= new InitialGuidanceManager();

        private readonly LogService _logService = LogService.Instance;
        private readonly MissileService _missileService = MissileService.Instance;
        private readonly TargetService _targetService = TargetService.Instance;

        private CancellationTokenSource? _cts;
        private IGuidanceState? _currentState;
        private int _currentSeq = 0;
        private Missile? _currentMissile;
        private double _currentProgress = 0;
        private bool _abortHandled = false;
        private int _curyaw = 0;
        private InitialGuidanceManager() { }

        private readonly List<string> Steps = new()
        {
            "전원 인가",
            "BIT",
            "항법 장치 정렬",
            "암호화 키 전달",
            "열전지 점화",
            "초기 PIP 전달",
            "발사 명령",
            "초기유도"
        };
        private int GetStepIndex(string name)
        {
            return Steps.IndexOf(name) + 1;  // 1-based
        }

        // ================================================================
        // 📍 메인 실행 진입점
        // ================================================================
        public async Task StartAsync(Missile missile)
        {
            if (_cts != null)
            {
                _logService.AddLog(MessageType.System, "이미 발사 절차가 진행 중입니다.");
                return;
            }

            _currentMissile = missile;
            _cts = new CancellationTokenSource();
            _currentProgress = 0;
            _logService.AddLog(MessageType.System, $"[{missile.Id}] 발사 절차 시작됨");

            _currentState = new PowerOnState(this, missile);
            await RunStateMachineAsync(_cts.Token);
        }

        private async Task RunStateMachineAsync(CancellationToken token)
        {
            try
            {
                Debug.WriteLine($"[StateMachine Start] {DateTime.Now:HH:mm:ss}");
                while (_currentState != null)
                {
                    _currentSeq = GetSeqFromState(_currentState);
                    await _currentState.EnterAsync(token);



                    _currentState = _currentState.NextState;
                }
                Debug.WriteLine($"[StateMachine Start] {DateTime.Now:HH:mm:ss}");
            }
            catch (TaskCanceledException)
            {
                _missileService.CancelLaunch();
            }
            catch (Exception ex)
            {
                _missileService.CancelLaunch();
                _logService.AddLog(MessageType.System, $"오류 발생: {ex.Message}");
            }
            finally
            {
                _cts = null;
                _currentState = null;
                _currentMissile = null;
                _abortHandled = false;
            }
        }

        // ================================================================
        // 📍 Abort 및 Progress 제어
        // ================================================================
        public void Abort()
        {
            if (_cts == null || _currentMissile == null)
            {
                return;
            }

            _cts.Cancel();
            PerformAbortSequence(_currentMissile, _currentSeq, false); // 현재 seq 판단은 FSM 내부에서 처리
        }

        internal void PerformAbortSequence(Missile missile, int seq, bool isSystem)
        {
            if (_abortHandled)
                return;

            _abortHandled = true;
            try
            {
                if (seq < 5)
                {
                    _logService.AddLog(MessageType.System, $"[{missile.Id}] 발사취소 절차 실행");

                    string senderId = "C001";
                    string receiverId = $"M{int.Parse(missile.Id):000}";
                    var header = new HeaderPacket(senderId, receiverId, (uint)seq, (byte)0);

                    BasePacket abortMsg = seq == 4
                        ? new KeyExchangeMessage(header)
                        : new InitialGuidanceMessage(header);

                    byte[] packet = abortMsg.Serialize();

                    var linkConfig = new LauncherLinkConfig();
                    if (linkConfig.TryGetLink(missile, out var link))
                    {
                        //using var client = new UdpClient(link.rxPort);
                        //var ep = new IPEndPoint(IPAddress.Parse(link.txIp), link.txPort);
                        //client.Send(packet, packet.Length, ep);
                    }

                    _missileService.UpdateMissileState(MissileState.Launching, MissileState.LaunchReady);
                    if (isSystem == true) _logService.AddLog(MessageType.System, $"[{missile.Id}] 가역 상태 오류 -> 대기 처리");
                    else _logService.AddLog(MessageType.System, $"사용자 입력으로 폭파 처리");
                }
                else
                {
                    _missileService.UpdateMissileState(MissileState.Launching, MissileState.Abort);
                    if (isSystem == true) _logService.AddLog(MessageType.System, $"[{missile.Id}] 비가역 상태 오류 -> 비상폭파 처리");
                }

                _cts?.Cancel();
                WeakReferenceMessenger.Default.Send(new LaunchEndMessage(missile.Id)); // Todo: 따라올 수 있는지
            }
            catch (Exception ex)
            {
                _logService.AddLog(MessageType.System, $"Abort 절차 중 오류: {ex.Message}");
            }

        }

        private int GetSeqFromState(IGuidanceState state)
        {
            return state switch
            {
                PowerOnState => 1,
                BitCheckState => 2,
                AlignState => 3,
                KeyState => 4,
                PipCalculationState => 6,
                LaunchState => 7,
                _ => 0
            };
        }

        internal interface IGuidanceState
        {
            Task EnterAsync(CancellationToken token);
            Task ExitAsync(CancellationToken token);
            IGuidanceState? NextState { get; }
        }

        internal abstract class BaseGuidanceState : IGuidanceState
        {
            protected readonly InitialGuidanceManager _manager;
            protected readonly Missile _launchingMissile;
            private readonly LauncherLinkConfig _linkConfig = new LauncherLinkConfig();

            public abstract string Name { get; }
            public virtual IGuidanceState? NextState { get; set; }

            protected BaseGuidanceState(InitialGuidanceManager manager, Missile missile)
            {
                _manager = manager;
                _launchingMissile = missile;
            }
            private async Task PreciseDelay(int milliseconds, CancellationToken token)
            {
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < milliseconds)
                {
                    token.ThrowIfCancellationRequested();
                    await Task.Delay(1, token); // CPU 점유 방지용 최소 슬립
                }
            }
            public virtual async Task EnterAsync(CancellationToken token)
            {
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
                    Missile msl = InitialGuidanceManager.Instance._currentMissile;
                    if (msl != null)
                    {

                        Debug.WriteLine($"YawRaw={(ushort)msl.YawRaw}");
                        _manager._logService.AddLog(MessageType.System, "언리얼발사 절차 완료");
                        SendToUE5.SendLaunchSignal("C001", "C002", 1, msl.Id, (ushort)msl.YawRaw);
                    }


                    WeakReferenceMessenger.Default.Send(new LaunchProgressMessage(Name, idx, false));
                }
                else
                    WeakReferenceMessenger.Default.Send(new LaunchProgressMessage(Name, idx, false));

                await PreciseDelay(500, token);
            }

            public virtual async Task ExitAsync(CancellationToken token)
            {
                int idx = _manager.GetStepIndex(Name);
                WeakReferenceMessenger.Default.Send(new LaunchProgressMessage(Name, idx, true));
                _manager._logService.AddLog(MessageType.System, $"{Name} 완료");
                await PreciseDelay(200, token);
            }

            protected async Task<bool> SendAndWaitForAck(Missile missile, int seq, int? msgSize = 0, byte[]? body = null)
            {
                if (!_linkConfig.TryGetLink(missile, out var link))
                {
                    _manager._logService.AddLog(MessageType.System, $"[{missile.Id}] 링크 설정이 없습니다.");
                    return false;
                }

                try
                {
                    string senderId = "C001";
                    string receiverId = $"M{int.Parse(missile.Id):000}";
                    var header = new HeaderPacket(senderId, receiverId, (uint)seq, (byte)msgSize);
                    MslKeyPacket mslKey = null;

                    BasePacket message = seq switch
                    {
                        4 => new KeyExchangeMessage(header),
                        6 => new InitialPipMessage(header),
                        _ => new InitialGuidanceMessage(header)
                    };

                    if (message is KeyExchangeMessage keyMsg && body != null)
                    {
                        keyMsg.SetEncryptionKey(body);

                        var EncryptionKey = new byte[32];
                        Array.Copy(body, EncryptionKey, Math.Min(32, body.Length));

                        mslKey = new MslKeyPacket(header, header.DestId, EncryptionKey);
                    }
                    else if (message is InitialPipMessage pipMsg && body != null)
                    {
                        pipMsg.SetPIP(
                            BitConverter.ToInt32(body, 0),
                            BitConverter.ToInt32(body, 4),
                            BitConverter.ToInt32(body, 8)
                        );
                    }

                    using var client = new UdpClient(link.RxPort);
                    var packet = message.Serialize();
                    var sendEp = new IPEndPoint(IPAddress.Parse(link.TxIp), link.TxPort);

                    _manager._logService.AddLog(MessageType.System,
                        $"[II-{seq:D4}] 송신 시작 ({missile.Id}) → {link.TxIp}:{link.TxPort}");
                    await client.SendAsync(packet, packet.Length, sendEp);

                    if (seq == 4)
                    {
                        packet = mslKey.Serialize();
                        _manager._logService.AddLog(MessageType.System,
                       $"[II-{seq:D4}] 송신 시작 ({missile.Id}) → {link.TxIp}:{link.TxPort}");
                        await client.SendAsync(packet, packet.Length, sendEp);
                    }

                    // 30초 타임아웃 설정
                    var recvTask = client.ReceiveAsync();
                    if (await Task.WhenAny(recvTask, Task.Delay(30000)) == recvTask)
                    {
                        var recvResult = recvTask.Result;
                        var response = ResponseMessage.FromBytes(recvResult.Buffer);

                        if (response.Header.Seq == seq)
                        {
                            _manager._logService.AddLog(MessageType.System,
                                $"[II-{seq:D4}] 응답 수신 ← {recvResult.RemoteEndPoint.Address}:{recvResult.RemoteEndPoint.Port}");
                            return true;
                        }

                        _manager._logService.AddLog(MessageType.System,
                            $"[II-{seq:D4}] 응답 시퀀스 불일치");
                        return false;
                    }
                    else
                    {
                        _manager._logService.AddLog(MessageType.System, $"[II-{seq:D4}] 응답 타임아웃");
                        return false;
                    }
                }
                catch (SocketException ex)
                {
                    _manager._logService.AddLog(MessageType.System, $"[II-{seq:D4}] 통신 오류: {ex.Message}");
                    return false;
                }
            }


            protected void HandleFailure(Missile missile, int seq)
            {
                _manager._logService.AddLog(MessageType.System, $"{Name} 단계 실패 — Abort 절차 실행");
                _manager.PerformAbortSequence(missile, seq, true);
            }
        }

        private class PowerOnState : BaseGuidanceState
        {
            public override string Name => "전원 인가";
            //public override IGuidanceState NextState => new BitCheckState(_manager, _launchingMissile);
            public override IGuidanceState? NextState { get; set; }

            public PowerOnState(InitialGuidanceManager manager, Missile missile) : base(manager, missile) { NextState = new BitCheckState(_manager, _launchingMissile); }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);
                _manager._missileService.UpdateMissileState(MissileState.LaunchReady, MissileState.Launching);
                WeakReferenceMessenger.Default.Send(new MissileLaunchMessage(_launchingMissile.Id));
                bool ok = await SendAndWaitForAck(_launchingMissile, seq: 1);
                if (!ok)
                {
                    HandleFailure(_launchingMissile, seq: 1);
                    NextState = null;
                    return;
                }
                await base.ExitAsync(token);
            }
        }



        private class BitCheckState : BaseGuidanceState
        {
            public override string Name => "BIT";
            public override IGuidanceState? NextState { get; set; }
            public BitCheckState(InitialGuidanceManager manager, Missile missile) : base(manager, missile) { NextState = new AlignState(_manager, _launchingMissile); }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);

                bool ok = await SendAndWaitForAck(_launchingMissile, seq: 2);
                if (!ok)
                {
                    HandleFailure(_launchingMissile, seq: 2);
                    NextState = null;
                    return;
                }

                await base.ExitAsync(token);
            }
        }

        private class AlignState : BaseGuidanceState
        {
            public override string Name => "항법 장치 정렬";
            public override IGuidanceState? NextState { get; set; }
            public AlignState(InitialGuidanceManager manager, Missile missile) : base(manager, missile) { NextState = new KeyState(_manager, _launchingMissile); }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);
                bool ok = await SendAndWaitForAck(_launchingMissile, seq: 3);
                if (!ok)
                {
                    HandleFailure(_launchingMissile, seq: 3);
                    NextState = null;
                    return;
                }
                await base.ExitAsync(token);
            }
        }
        private class KeyState : BaseGuidanceState
        {
            public override string Name => "암호화 키 전달";
            public override IGuidanceState? NextState { get; set; }
            public KeyState(InitialGuidanceManager manager, Missile missile) : base(manager, missile) { NextState = new IgnitionState(_manager, _launchingMissile); }
            private byte[] GenerateSessionKey()
            {
                var key = new byte[32];
                RandomNumberGenerator.Fill(key);
                return key;
            }
            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);

                byte[] key = GenerateSessionKey();
                bool ok = await SendAndWaitForAck(_launchingMissile, seq: 4, msgSize: 32, key);
                if (!ok)
                {
                    HandleFailure(_launchingMissile, seq: 4);
                    NextState = null;
                    return;
                }
                await base.ExitAsync(token);
            }
        }


        private class IgnitionState : BaseGuidanceState
        {
            public override string Name => "열전지 점화";
            public override IGuidanceState? NextState { get; set; }
            public IgnitionState(InitialGuidanceManager manager, Missile missile) : base(manager, missile) { NextState = new PipCalculationState(_manager, _launchingMissile); }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);
                bool ok = await SendAndWaitForAck(_launchingMissile, seq: 5);
                if (!ok)
                {
                    HandleFailure(_launchingMissile, seq: 5);
                    NextState = null;
                    return;
                }
                await base.ExitAsync(token);
            }
        }

        private class PipCalculationState : BaseGuidanceState
        {
            public override string Name => "초기 PIP 전달";
            public override IGuidanceState? NextState { get; set; }
            public PipCalculationState(InitialGuidanceManager manager, Missile missile) : base(manager, missile) { NextState = new LaunchState(_manager, _launchingMissile); }


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
                // 1️ 위경도 → 평면 좌표
                var (tx, ty) = LatLonToXY(target.CurLoc.Lat, target.CurLoc.Lon, lat0, lon0);
                var (mx, my) = LatLonToXY(missileLat, missileLon, lat0, lon0);

                // 2️ Yaw (1e7으로 스케일 보정)
                double yawDeg = (target.Yaw) / 100.0;
                double theta = yawDeg * Math.PI / 180.0;

                // 3️ 타겟 진행 방향 단위벡터 * 속도(m/s)
                double dx = target.Speed * Math.Sin(theta);
                double dy = target.Speed * Math.Cos(theta);

                // 4️ 교차 시간 t 탐색 (선형 탐색)
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

                // 5️최종 PIP 계산 (1e7 스케일 적용 후 정수 변환)
                int pipX = (int)Math.Round((tx + dx * t));
                int pipY = (int)Math.Round((ty + dy * t));

                return (pipX, pipY);
            }

            private byte[] BuildPipBody(int x, int y, int z)
            {
                var body = new List<byte>(12);
                body.AddRange(BitConverter.GetBytes(x));
                body.AddRange(BitConverter.GetBytes(y));
                body.AddRange(BitConverter.GetBytes(z));
                return body.ToArray();
            }

            public override async Task EnterAsync(CancellationToken token)
            {
                await base.EnterAsync(token);

                // Todo: X Y Z 변환해서 보내야함
                char targetId = _launchingMissile.TargetId[0];
                // 1. PIP를 구한다. (현재 표적 위 경 고도
                var target = _manager._targetService.GetTarget(targetId);

                if (target == null)
                {
                    NextState = null;
                    return;
                }

                (int x, int y) targetXY = ComputePIP(
                     target: target,
                     missileLat: _launchingMissile.Latitude,
                     missileLon: _launchingMissile.Longitude,
                     missileSpeed: 1000,
                     lat0: _launchingMissile.Latitude,
                     lon0: _launchingMissile.Longitude

                );

                byte[] pip = BuildPipBody(targetXY.x, targetXY.y, 10);

                (double lat, double lon) targetLatLon = XYToLatLon(targetXY.x, targetXY.y,
                    _launchingMissile.Latitude, _launchingMissile.Longitude);

                _launchingMissile.PIP = new PIP(targetLatLon.lat, targetLatLon.lon, 10);

                var (mx, my) = LatLonToXY(
                    _launchingMissile.Latitude,
                    _launchingMissile.Longitude,
                    _launchingMissile.Latitude,
                    _launchingMissile.Longitude);

                double dx = targetXY.x - mx;
                double dy = targetXY.y - my;

                // 🔥 yaw 계산 (북=0°, 동=90°, 서=270°)
                double rawYaw = Math.Atan2(dx, dy) * (180.0 / Math.PI);

                // 🔥 음수 값을 0~360 범위로
                if (rawYaw < 0)
                    rawYaw += 360.0;

                ushort yawRaw = (ushort)(rawYaw * 100);
                _launchingMissile.YawRaw = yawRaw;
                Debug.WriteLine($"YawRaw={yawRaw}");
                bool ok = await SendAndWaitForAck(_launchingMissile, seq: 6, msgSize: 12, body: pip);
                if (!ok)
                {
                    HandleFailure(_launchingMissile, seq: 6);
                    return;
                }

                await base.ExitAsync(token);
            }
        }


        private class LaunchState : BaseGuidanceState
        {
            public override string Name => "발사 명령";
            public override IGuidanceState? NextState { get; set; }
            public LaunchState(InitialGuidanceManager manager, Missile missile) : base(manager, missile) { NextState = null; }

            public override async Task EnterAsync(CancellationToken token)
            {

                var missileId = _manager._missileService.UpdateMissileState(MissileState.Launching, MissileState.InitialGuidance); // 발사중 -> 초기유도로 전환


                if (missileId != null)

                {
                    _manager._logService.AddLog(MessageType.System, $"{missileId} 초기유도 단계로 전환됨");
                }

                await base.EnterAsync(token);
                bool ok = await SendAndWaitForAck(_launchingMissile, seq: 7);
                if (!ok)
                {
                    HandleFailure(_launchingMissile, seq: 7);
                    return;
                }
                _launchingMissile.flightTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                await base.ExitAsync(token);
            }

        }

        
        private class LauncherLinkConfig
        {
            public record LinkEndPoints(string TxIp, int TxPort, string RxIp, int RxPort);

            private readonly Dictionary<string, LinkEndPoints> _configMap;

            public LauncherLinkConfig()
            {
                var network = AppConfig.Network;
                _configMap = new Dictionary<string, LinkEndPoints>();

                foreach (var item in network.LauncherLinks)
                {
                    if (string.IsNullOrWhiteSpace(item.MissileId))
                        continue;

                    _configMap[item.MissileId] = new LinkEndPoints(
                        item.TxIp,
                        item.TxPort,
                        item.RxIp,
                        item.RxPort
                    );
                }
            }

            // Missile.Id 가 "1", "2" 이런 형태라고 가정
            public bool TryGetLink(Missile missile, out LinkEndPoints link)
            {
                return _configMap.TryGetValue(missile.Id, out link);
            }
        }
    }
}
