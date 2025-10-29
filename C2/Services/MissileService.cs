using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using C2.Models;
using System.Windows;
using GMap.NET;

namespace C2.Services
{
    public class MissileService : ObservableObject
    {
        private readonly List<Missile> missiles;
        private readonly Dictionary<string, MissileController> controllers;
        private readonly Dictionary<string, PIP> pips = new();
        private readonly Random rand = new();

        //public MissileService()
        //{
        //    missiles = new List<Missile>
        //{
        //    new Missile("MSL-01", (int)(37.5665 * 1e7), (int)(126.9780 * 1e7), (short)0, MissileState.LaunchReady),
        //    new Missile("MSL-02", (int)(36.3504 * 1e7), (int)(127.3845 * 1e7), (short)250, MissileState.InitialGuidance, "TGT-01"),
        //    new Missile("MSL-03", (int)(35.1796 * 1e7), (int)(129.0756 * 1e7), (short)480, MissileState.MidGuidance, "TGT-02"),
        //    new Missile("MSL-04", (int)(35.9078 * 1e7), (int)(127.7669 * 1e7), (short)720, MissileState.TerminalGuidance, "TGT-03"),
        //};

        //    controllers = missiles.ToDictionary(
        //        m => m.Id,
        //        m => new MissileController(m)
        //    );
        //}

        //테스트용
        public MissileService()
        {
            // 미사일 초기 생성
            missiles = new List<Missile>();
            controllers = new Dictionary<string, MissileController>();


            for (int i = 1; i <= 4; i++)
            {
                // 시작 위치
                double startLat = rand.NextDouble() * (36 - 35) + 35;
                double startLon = rand.NextDouble() * (127 - 125) + 125;

                // 목표 위치
                double PIPLat = rand.NextDouble() * (39 - 38) + 38;
                double PIPLon = rand.NextDouble() * (127 - 125) + 125;

                var missile = new Missile(
                    id: $"MSL-{i:00}",
                    latitudeRaw: (int)(startLat * 1e7),
                    longitudeRaw: (int)(startLon * 1e7),
                    altitude: (short)0,
                    state: MissileState.MidGuidance
                );

                // 초기 PIP 생성하고 
                var pip = new PIP(missile.Id, PIPLat, PIPLon);
                missiles.Add(missile);
                pips[missile.Id] = pip;

                var controller = new MissileController(missile);
                controller.SetPIP(pip);
                controllers[missile.Id] = controller;


            }
            foreach (var controller in controllers.Values)
                _ = Task.Run(() => controller.SimulateFlightAsync(1000));

        }
        public Missile? GetMissile(string id) => missiles.FirstOrDefault(m => m.Id == id);
        public List<Missile> GetAllMissiles() => missiles;
        public PIP? GetPIP(string missileId) => pips.GetValueOrDefault(missileId);
        public MissileController? GetController(string id) => controllers.GetValueOrDefault(id);

    }
}