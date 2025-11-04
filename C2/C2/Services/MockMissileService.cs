using C2.Models;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace C2.Services
{
    public class MockMissileService
    {
        private static MockMissileService _instance;
        public static MockMissileService Instance => _instance ??= new MockMissileService();

        public List<MissileController> missileControllers { get; set; } = new();
        public List<PIP> PIPs { get; set; } = new();

        private readonly Random rand = new();
        private bool _isRunning;

        private MockMissileService()
        {
            // ✅ 타겟 서비스 먼저 초기화
            var targets = MockTargetService.Instance.TargetControllers;
            int missileCount = Math.Min(targets.Count, 4);

            for (int i = 0; i < missileCount; i++)
            {
                var target = targets[i].Target;

                // 🔹 미사일 시작 위치 (35~36, 126~128)
                double startLat = rand.NextDouble() * (36 - 35) + 35;
                double startLon = rand.NextDouble() * (128 - 126) + 126;

                // 🔹 목표 위치 = 표적의 EndLoc
                double pipLat = target.EndLoc.Lat;
                double pipLon = target.EndLoc.Lon;

                var missile = new Missile(
                    id: $"MSL-{i + 1:00}",
                    latitudeRaw: (int)(startLat * 1e7),
                    longitudeRaw: (int)(startLon * 1e7),
                    altitude: (short)0,
                    state: MissileState.MidGuidance,
                    targetId: target.Id.ToString()
                );

                var pip = new PIP(missile.Id, pipLat, pipLon);
                var controller = new MissileController(missile) { PIP = pip };

                missileControllers.Add(controller);
                PIPs.Add(pip);
            }

            // ✅ 하나의 루프로 전체 미사일 이동 시뮬레이션
            _ = Task.Run(() => SimulateAllAsync(2500));
        }

        private async Task SimulateAllAsync(int speed)
        {
            if (_isRunning) return;
            _isRunning = true;

            DateTime lastUpdate = DateTime.UtcNow;

            while (true)
            {
                await Task.Delay(10); // 100Hz
                double elapsed = (DateTime.UtcNow - lastUpdate).TotalSeconds;
                lastUpdate = DateTime.UtcNow;

                foreach (var controller in missileControllers)
                    controller.UpdateStep(elapsed, speed);
            }
        }
    }

    public class MissileController
    {
        public Missile Missile { get; }
        public PIP? PIP { get; set; }

        private double lat;
        private double lon;

        public MissileController(Missile missile)
        {
            Missile = missile;
            lat = missile.LatitudeRaw / 1e7;
            lon = missile.LongitudeRaw / 1e7;
        }

        /// <summary>
        /// 각 미사일을 주기적으로 이동시키는 루프
        /// </summary>
        public void UpdateStep(double elapsed, int speed)
        {
            if (PIP == null) return;

            double targetLat = PIP.Latitude;
            double targetLon = PIP.Longitude;

            // 남은 거리 계산
            double remaining = HaversineDistance(lat, lon, targetLat, targetLon);
            if (remaining < 1.0)
            {
                Missile.State = MissileState.TerminalGuidance;
                return;
            }

            // 이동 거리 = 속도 × 시간
            double moveDist = speed * elapsed;
            double ratio = moveDist / remaining;
            if (ratio > 1) ratio = 1;

            double oldLat = lat;
            double oldLon = lon;

            // 위경도 보간 이동
            lat += (targetLat - lat) * ratio;
            lon += (targetLon - lon) * ratio;

            // 실제 위치 갱신
            Missile.LatitudeRaw = (int)(lat * 1e7);
            Missile.LongitudeRaw = (int)(lon * 1e7);

            // 🔹 진행 방향 (Yaw) 계산
            // 🔹 진행 방향 (Yaw) 계산
            double dLon = (lon - oldLon) * Math.PI / 180.0;
            double y = Math.Sin(dLon) * Math.Cos(lat * Math.PI / 180.0);
            double x = Math.Cos(oldLat * Math.PI / 180.0) * Math.Sin(lat * Math.PI / 180.0)
                     - Math.Sin(oldLat * Math.PI / 180.0) * Math.Cos(lat * Math.PI / 180.0) * Math.Cos(dLon);
            double brng = Math.Atan2(y, x) * 180.0 / Math.PI;
            Missile.YawRaw = (short)(((brng + 360.0) % 360.0) * 100);
        }

        // 거리 계산 (Haversine)
        private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0;
            double dLat = (lat2 - lat1) * Math.PI / 180.0;
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }

    // ✅ Mock PIP 클래스
    public class PIP
    {
        public string MissileId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public PointLatLng Position => new PointLatLng(Latitude, Longitude);

        public PIP(string missileId, double latitude, double longitude)
        {
            MissileId = missileId;
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
