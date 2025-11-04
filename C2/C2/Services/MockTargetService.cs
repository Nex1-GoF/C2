using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using C2.Models;

namespace C2.Services
{
    public class MockTargetService
    {
        private static MockTargetService _instance;
        public static MockTargetService Instance => _instance ??= new MockTargetService();

        public List<TargetController> TargetControllers { get; set; } = new();
        private readonly Random rand = new();
        private bool _isRunning;

        private MockTargetService()
        {
            for (int i = 1; i <= 4; i++)
            {
                // 🔹 시작 / 종료 위치 랜덤
                double startLat = rand.NextDouble() * (42 - 41) + 41;
                double startLon = rand.NextDouble() * (127 - 125) + 125;
                double endLat = rand.NextDouble() * (38 - 37) + 37;
                double endLon = rand.NextDouble() * (128 - 126) + 126;

                int altitude = rand.Next(500, 1500);  // m
                int speed = 2500;      // m/s

                var target = new Target
                {
                    Id = (char)('A' + i - 1),
                    DetectedType = 'A',
                    Speed = speed,
                    Altitude = altitude,
                    DetectTime = DateTime.Now,
                    EndLoc = (endLat, endLon)
                };

                SetPrivateProperty(target, "CurLoc", (startLat, startLon));
                SetPrivateProperty(target, "IsMoving", true);

                TargetControllers.Add(new TargetController(target));
            }

            // ✅ 모든 타겟을 하나의 루프로 갱신
            _ = Task.Run(() => SimulateAllTargetsAsync());
        }

        private async Task SimulateAllTargetsAsync()
        {
            if (_isRunning) return;
            _isRunning = true;

            DateTime lastUpdate = DateTime.UtcNow;

            while (true)
            {
                await Task.Delay(10); // 100Hz
                double elapsed = (DateTime.UtcNow - lastUpdate).TotalSeconds;
                lastUpdate = DateTime.UtcNow;

                foreach (var controller in TargetControllers)
                {
                    controller.UpdateStep(elapsed);
                }
            }
        }

        private static void SetPrivateProperty<T>(Target target, string propertyName, T value)
        {
            var prop = typeof(Target).GetProperty(propertyName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            prop?.SetValue(target, value);
        }
    }

    public class TargetController
    {
        public Target Target { get; }
        private double lat;
        private double lon;

        public TargetController(Target target)
        {
            Target = target;
            lat = target.CurLoc.Lat;
            lon = target.CurLoc.Lon;
        }

        /// <summary>
        /// 외부 루프(공용 Task)에서 주기적으로 호출됨
        /// </summary>
        public void UpdateStep(double elapsed)
        {

            double targetLat = Target.EndLoc.Lat;
            double targetLon = Target.EndLoc.Lon;
            double remaining = HaversineDistance(lat, lon, targetLat, targetLon);

            if (remaining < 1.0)
            {
                SetPrivateProperty(Target, "IsMoving", false);
                return;
            }

            double moveDist = Target.Speed * elapsed;
            double ratio = moveDist / remaining;
            if (ratio > 1) ratio = 1;

            double newLat = lat + (targetLat - lat) * ratio;
            double newLon = lon + (targetLon - lon) * ratio;

            SetPrivateProperty(Target, "CurLoc", (newLat, newLon));

            int yaw = (int)ComputeYaw(lat, lon, newLat, newLon);
            var yawField = typeof(Target).GetProperty("Yaw",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            yawField?.SetValue(Target, yaw);

            lat = newLat;
            lon = newLon;
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

        private static double ComputeYaw(double lat1, double lon1, double lat2, double lon2)
        {
            double dLon = (lon2 - lon1) * Math.PI / 180.0;
            double y = Math.Sin(dLon) * Math.Cos(lat2 * Math.PI / 180.0);
            double x = Math.Cos(lat1 * Math.PI / 180.0) * Math.Sin(lat2 * Math.PI / 180.0)
                     - Math.Sin(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) * Math.Cos(dLon);
            double brng = Math.Atan2(y, x) * 180.0 / Math.PI;
            return (brng + 360.0) % 360.0;
        }

        private static void SetPrivateProperty<T>(Target target, string propertyName, T value)
        {
            var prop = typeof(Target).GetProperty(propertyName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            prop?.SetValue(target, value);
        }
    }
}
