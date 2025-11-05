using C2.Models;
using GMap.NET;
using System;
using System.Collections.Generic;
using System.Linq;
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
            _ = Task.Run(() => SimulateAllAsync(2500));
        }

        /// <summary>
        /// 교전할당 버튼이 눌릴 때 미사일 하나 생성 (이동하지 않음)
        /// </summary>
        public bool TryAssignMissile()
        {
            var targets = MockTargetService.Instance.TargetControllers;
            if (missileControllers.Count >= 4 || targets.Count == 0)
                return false;

            int index = missileControllers.Count;
            var target = targets[index % targets.Count].Target;

            double startLat = rand.NextDouble() * (36 - 35) + 35;
            double startLon = rand.NextDouble() * (128 - 126) + 126;
            double pipLat = target.EndLoc.Lat;
            double pipLon = target.EndLoc.Lon;

            var missile = new Missile(
                id: $"MSL-{index + 1:00}",
                latitudeRaw: (int)(startLat * 1e7),
                longitudeRaw: (int)(startLon * 1e7),
                altitude: (short)0,
                state: MissileState.LaunchReady, // ⬅️ 아직 발사 안됨
                targetId: target.Id.ToString()
            );

            var pip = new PIP(missile.Id, pipLat, pipLon);
            var controller = new MissileController(missile)
            {
                PIP = pip,
                IsLaunched = false
            };

            missileControllers.Add(controller);
            PIPs.Add(pip);

            return true;
        }

        /// <summary>
        /// 발사 버튼 클릭 시, 아직 발사되지 않은 미사일 하나를 발사시킴
        /// </summary>
        public bool TryLaunchNextMissile()
        {
            var next = missileControllers.FirstOrDefault(m => !m.IsLaunched);
            if (next == null) return false; // 🚫 남은 미사일 없음

            next.IsLaunched = true;
            next.Missile.State = MissileState.MidGuidance;
            return true;
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
                {
                    // ⬇️ 발사된 미사일만 이동
                    if (controller.IsLaunched)
                        controller.UpdateStep(elapsed, speed);
                }
            }
        }
    }

    public class MissileController
    {
        public Missile Missile { get; }
        public List<(double Lat, double Lon)> PathHistory { get; } = new();
        public PIP? PIP { get; set; }

        public bool IsLaunched { get; set; } = false; // ⬅️ 발사 여부 추가

        private double lat;
        private double lon;

        public MissileController(Missile missile)
        {
            Missile = missile;
            lat = missile.LatitudeRaw / 1e7;
            lon = missile.LongitudeRaw / 1e7;
        }

        public void UpdateStep(double elapsed, int speed)
        {
            if (PIP == null) return;

            double targetLat = PIP.Latitude;
            double targetLon = PIP.Longitude;

            double remaining = HaversineDistance(lat, lon, targetLat, targetLon);
            if (remaining < 1.0)
            {
                Missile.State = MissileState.TerminalGuidance;
                return;
            }

            double moveDist = speed * elapsed;
            double ratio = moveDist / remaining;
            if (ratio > 1) ratio = 1;

            double oldLat = lat;
            double oldLon = lon;

            lat += (targetLat - lat) * ratio;
            lon += (targetLon - lon) * ratio;
            PathHistory.Add((lat, lon));

            Missile.LatitudeRaw = (int)(lat * 1e7);
            Missile.LongitudeRaw = (int)(lon * 1e7);

            // 진행방향 계산
            double dLon = (lon - oldLon) * Math.PI / 180.0;
            double y = Math.Sin(dLon) * Math.Cos(lat * Math.PI / 180.0);
            double x = Math.Cos(oldLat * Math.PI / 180.0) * Math.Sin(lat * Math.PI / 180.0)
                     - Math.Sin(oldLat * Math.PI / 180.0) * Math.Cos(lat * Math.PI / 180.0) * Math.Cos(dLon);
            double brng = Math.Atan2(y, x) * 180.0 / Math.PI;
            Missile.YawRaw = (short)(((brng + 360.0) % 360.0) * 100);
        }

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
