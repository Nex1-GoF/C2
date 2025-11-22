using C2.Messages;
using C2.Models;
using C2.Network;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace C2.Services
{
    public class MissileService
    {
        private static MissileService _instance;
        public static MissileService Instance => _instance ??= new MissileService();

        // 발사대 위치 (서울 시청 인근)
        public (int latitude, int longitude, short altitude) C2Points =
            ((int)(37.5665 * 1e7), (int)(126.9780 * 1e7), (short)(38));
        private readonly LogService _logService;

        // Dictionary로 변경 (Key: Missile ID)
        private readonly Dictionary<string, Missile> _missiles = new();

        // 선택된 미사일 (단일)
        public List<Missile> SelectedMissiles { get; private set; } = new();

        private readonly object _lock = new();

        private readonly AbortManager _abortManager;

        private MissileService()
        {
            _logService = LogService.Instance;
            _abortManager = AbortManager.Instance;
            // 초기 미사일 4기 등록
            for (int i = 1; i <= 4; i++)
            {
                var missile = new Missile(
                    id: $"{i:0}",
                    latitudeRaw: C2Points.latitude,
                    longitudeRaw: C2Points.longitude,
                    altitude: C2Points.altitude,
                    speed: 1500
                );
                _missiles[missile.Id] = missile;
            }
        }

        // 전체 미사일 반환 (읽기 전용 Dictionary)
        public List<Missile> GetAllMissiles() => _missiles.Values.ToList();

        // 특정 미사일 직접 가져오기
        public Missile? GetMissile(string id)
        {
            _missiles.TryGetValue(id, out var missile);
            return missile;
        }

        // 교전할당 시 사용
        public string AssignTarget(string targetId)
        {
            foreach (var missile in GetAllMissiles()) {

                if (missile.State != MissileState.LaunchReady) continue;
                if (missile.TargetId != null) continue;


                _missiles[missile.Id].TargetId = targetId;



                return missile.Id;
            
            }
            return "";

        }

        public string? UpdateMissileState(MissileState fromState, MissileState toState)
        {
            var missile = GetAllMissiles()
                .FirstOrDefault(m => m.State == fromState && m.TargetId != null);

            if (missile == null)
                return null;

            missile.State = toState;
            return missile.Id;
        }
        
        public bool AnyRemaining()
        {
            var missile = GetAllMissiles().FirstOrDefault(m => m.State == MissileState.LaunchReady && m.TargetId != null);
            if (missile == null) return false;
            return true;
        }
        public void CancelLaunch()
        {
            var missile = GetAllMissiles().FirstOrDefault(m => m.State == MissileState.Launching && m.TargetId != null);
            if (missile == null) return;
            missile.State = MissileState.LaunchReady;
        }
        public bool CanLaunch()
        {
            var missile = GetAllMissiles().FirstOrDefault(m => m.State == MissileState.LaunchReady);
            if (missile == null) return false;
            if(missile.TargetId == null) return false;
            return true;
        }

        // ✅ 미사일 선택
        public bool SelectMissiles(IEnumerable<string> ids)
        {
            SelectedMissiles.Clear();
            foreach (var id in ids)
            {
                if (_missiles.TryGetValue(id, out var missile))
                    SelectedMissiles.Add(missile);
            }

            WeakReferenceMessenger.Default.Send(new MissileSelectedMessage(null)); // or multiple msg
            return SelectedMissiles.Count > 0;
        }

        public void ClearMissiles()
        {
            SelectedMissiles.Clear();
            WeakReferenceMessenger.Default.Send(new MissileSelectedMessage(null));
        }

        public void ReceiveMissileData(Missile newData)
        {
            if (_missiles.TryGetValue(newData.Id, out var existing))
            {
                if (existing.State == MissileState.InitialGuidance && (newData.State == MissileState.MidGuidance || newData.State == MissileState.TerminalGuidance))
                {
                    WeakReferenceMessenger.Default.Send(new LaunchEndMessage(existing.Id));
                }
                existing.Update(newData);

                //Abort처리
                if (newData.State == MissileState.Abort)
                {   //Abort처리
                    if (!existing.IsAbort)
                    {
                        LogService _logService = LogService.Instance;
                        //_logService.AddLog(MessageType.System, "기폭");
                        //자폭인지 판별
                        if (!existing.IsSelfabort)//자폭이 아니라면 타겟요격임
                        {
                            //_logService.AddLog(MessageType.System, "폭파");
                            if (!string.IsNullOrEmpty(existing.TargetId))
                            {
                                //_logService.AddLog(MessageType.System, "폭파신호");
                                _abortManager.AbortTarget(existing.TargetId[0]);
                            }
                        }
                        //if (!string.IsNullOrEmpty(existing.TargetId))
                        //{
                        //    _abortManager.AbortTarget(existing.TargetId[0]);
                        //}
                        //폭파처리
                        existing.IsAbort = true;
                        SendToUE5.SendDetonationSignal("C001", "C002", 3, existing.Id);
                        WeakReferenceMessenger.Default.Send(new MissileAbortMessage(existing.Id));
                    }
                }
            }
            else
            {
                // 초기 4기 생성된 경우에만 들어옴 (이미 있음)
                _missiles[newData.Id] = newData;
            }
        }
    }
}
