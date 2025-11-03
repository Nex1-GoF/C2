using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using C2.Models;

namespace C2.Services
{
    public class MissileService
    {
        private static MissileService _instance;

        public static MissileService Instance => _instance ??= new MissileService();
        public List<MissileController> missileControllers { get; set; } = new();
        private readonly Random rand = new();


        private MissileService()
        {
            // ✅ 테스트용 미사일 생성
            for (int i = 1; i <= 4; i++)
            {
                double startLat = rand.NextDouble() * (36 - 35) + 35;
                double startLon = rand.NextDouble() * (127 - 125) + 125;
                double pipLat = rand.NextDouble() * (39 - 38) + 38;
                double pipLon = rand.NextDouble() * (127 - 125) + 125;

                var missile = new Missile(
                    id: $"MSL-{i:00}",
                    latitudeRaw: (int)(startLat * 1e7),
                    longitudeRaw: (int)(startLon * 1e7),
                    altitude: (short)0,
                    state: MissileState.MidGuidance
                );

                var pip = new PIP(missile.Id, pipLat, pipLon);

                var controller = new MissileController(missile);
                controller.SetPIP(pip);

                missileControllers.Add(controller);

                // 비행 시뮬레이션 실행
                _ = Task.Run(() => controller.SimulateFlightAsync(1000));
            }
        }
    }
}
