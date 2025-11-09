using C2.Messages;
using C2.Models;
using CommunityToolkit.Mvvm.Messaging;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Interop;
using System.Windows.Media; // Color

namespace C2.Services
{
    public class MapService
    {
        private static MapService _instance;
        public static MapService Instance => _instance ??= new MapService();

        // ==============================
        // Map State
        // ==============================
        public PointLatLng Center { get; set; } = new PointLatLng(37.5665, 126.9780); // 서울 시청
        public double Distance { get; set; } = 250_000; // 250 km

        // ==============================
        // Domain Services
        // ==============================
        private readonly MissileService _missileService = MissileService.Instance;
        private readonly TargetService _targetService = TargetService.Instance;

        // 선택 상태(ID만 공개)
        public IReadOnlyList<string> SelectedMissileIds => _missileService.SelectedMissiles.Select(m => m.Id).ToList();
        public char? SelectedTargetId => _targetService.SelectedTarget?.Id;

        // ViewModel이 구독할 이벤트
        public event Action? SnapshotUpdated;
        public event Action? FocusChanged;

        private MapService()
        {
            WeakReferenceMessenger.Default.Register<MissileSelectedMessage>(this, (_, __) => FocusChanged?.Invoke());
            WeakReferenceMessenger.Default.Register<TargetSelectedMessage>(this, (_, __) => FocusChanged?.Invoke());
        }

        // ==============================
        // 외부 조회
        // ==============================
        public IEnumerable<Missile> GetMissiles() => _missileService.GetAllMissiles();
        public IEnumerable<Target> GetTargets() => _targetService.GetAllTargets();

        // ==============================
        // 클릭/선택 로직
        // ==============================
        public void OnMissileClicked(string missileId)
        {
            var selected = SelectedMissileIds.ToList();

            // ✅ 이미 선택된 미사일이면 → 전체 해제
            if (selected.Contains(missileId))
            {
                _missileService.ClearMissiles();
                _targetService.ClearTarget();
            }
            else
            {
                // ✅ 새 미사일 클릭 → 해당 미사일 선택 + 연결된 타겟 선택
                _missileService.SelectMissiles(new List<string> { missileId });

                var missile = _missileService.GetMissile(missileId);
                if (missile?.TargetId != null && missile.TargetId.Length > 0)
                {
                    _targetService.SelectTarget(missile.TargetId[0]);
                }
                else
                {
                    _targetService.ClearTarget();
                }
            }

            FocusChanged?.Invoke();
            SnapshotUpdated?.Invoke();
        }


        public void OnTargetClicked(char targetId)
        {
            // 동일 TargetId를 추적 중인 모든 미사일 선택
            var relatedMissiles = _missileService.GetAllMissiles()
                .Where(m => m.TargetId != null && m.TargetId[0] == targetId)
                .Select(m => m.Id)
                .ToList();

            if (SelectedTargetId == targetId)
            {
                _targetService.ClearTarget();
                _missileService.ClearMissiles();
            }
            else
            {
                _targetService.SelectTarget(targetId);
                _missileService.SelectMissiles(relatedMissiles);
            }

            FocusChanged?.Invoke();
            SnapshotUpdated?.Invoke();
        }

        // 주기 갱신
        public void Tick() => SnapshotUpdated?.Invoke();

        // ==============================
        // 렌더링용 데이터 제공
        // ==============================
        public IEnumerable<(string Id, string Kind, double Lat, double Lon, bool Focused, bool Visible)> GetMarkerSpecs()
        {
            var list = new List<(string, string, double, double, bool, bool)>();
            var selectedIds = SelectedMissileIds;

            // 미사일 + PIP
            foreach (var m in _missileService.GetAllMissiles())
            {
                if (m.State == MissileState.Abort) continue;
                bool focused = selectedIds.Contains(m.Id);
                list.Add((m.Id, "Missile", m.Latitude, m.Longitude, focused, true));
                
                if (m.PIP != null)
                {
                    // 포커스된 미사일의 PIP만 표시
                    bool pipVisible = focused;
                    list.Add(($"PIP::{m.Id}", "PIP", m.PIP.Latitude, m.PIP.Longitude, pipVisible, pipVisible));
                }
            }

            // 타겟
            foreach (var t in _targetService.GetAllTargets())
            {
                bool focused = (SelectedTargetId == t.Id);
                list.Add((t.Id.ToString(), "Target", t.CurLoc.Lat, t.CurLoc.Lon, focused, true));
            }

            return list;
        }

        // 여러 미사일 ↔ PIP, Target ↔ PIP 연결선
        public IEnumerable<(PointLatLng From, PointLatLng To, string Style)> GetLineSpecs()
        {
            var lines = new List<(PointLatLng, PointLatLng, string)>();
            var selectedIds = SelectedMissileIds;
            var target = _targetService.SelectedTarget;

            foreach (var mid in selectedIds)
            {
                var msl = _missileService.GetMissile(mid);
                if (msl?.PIP == null) continue;
                if(msl.State==MissileState.Abort) continue;

                // Missile ↔ PIP
                lines.Add((new PointLatLng(msl.Latitude, msl.Longitude),
                           new PointLatLng(msl.PIP.Latitude, msl.PIP.Longitude),
                           "DashedBlack"));

                // Target ↔ PIP (표적이 있을 경우)
                if (target != null)
                {
                    lines.Add((new PointLatLng(target.CurLoc.Lat, target.CurLoc.Lon),
                               new PointLatLng(msl.PIP.Latitude, msl.PIP.Longitude),
                               "DashedBlack"));
                }
            }

            return lines;
        }

        // 경로 (Path)
        public IEnumerable<(List<PointLatLng> Points, Color Color)> GetRouteSpecs()
        {
            var routes = new List<(List<PointLatLng>, Color)>();
            var selectedIds = SelectedMissileIds;

            // 여러 미사일 경로
            foreach (var mid in selectedIds)
            {
                var msl = _missileService.GetMissile(mid);
                if (msl?.PathHistory?.Count > 1)
                {
                    routes.Add((
                        msl.PathHistory.Select(p => new PointLatLng(p.Lat, p.Lon)).ToList(),
                        Colors.LightSkyBlue
                    ));
                }
            }

            // 단일 표적 경로
            var tgt = (SelectedTargetId != null) ? _targetService.GetTarget(SelectedTargetId.Value) : null;
            if (tgt?.PathHistory?.Count > 1)
            {
                routes.Add((
                    tgt.PathHistory.Select(p => new PointLatLng(p.Lat, p.Lon)).ToList(),
                    Colors.OrangeRed
                ));
            }

            return routes;
        }
    }
}
