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

namespace C2.Services
{
    public class MissileService : ObservableObject
    {
        private readonly List<Missile> missiles;
        private readonly Dictionary<string, MissileController> controllers;

        public MissileService()
        {
            missiles = new List<Missile>
        {
            new Missile("MSL-01", (int)(37.5665 * 1e7), (int)(126.9780 * 1e7), (short)0, MissileState.LaunchReady),
            new Missile("MSL-02", (int)(36.3504 * 1e7), (int)(127.3845 * 1e7), (short)250, MissileState.InitialGuidance, "TGT-01"),
            new Missile("MSL-03", (int)(35.1796 * 1e7), (int)(129.0756 * 1e7), (short)480, MissileState.MidGuidance, "TGT-02"),
            new Missile("MSL-04", (int)(35.9078 * 1e7), (int)(127.7669 * 1e7), (short)720, MissileState.TerminalGuidance, "TGT-03"),
        };

            controllers = missiles.ToDictionary(
                m => m.Id,
                m => new MissileController(m)
            );
        }

        public Missile? GetMissile(string id) => missiles.FirstOrDefault(m => m.Id == id);
        public List<Missile> GetAllMissiles() => missiles;
        public MissileController? GetController(string id) => controllers.GetValueOrDefault(id);

        // 예시: 전체 초기화
        public void ResetAll()
        {
            foreach (var controller in controllers.Values)
                controller.Reset();
        }
    }
}