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
    public partial class MissileService : ObservableObject
    {

        private List<Missile> missiles;

        public MissileService()
        {

            //missiles = new List<Missile>
            //{
            //    new Missile("MSL-01", 37.5665, 126.9780),
            //    new Missile("MSL-02", 35.1796, 129.0756),
            //    new Missile("MSL-03", 36.3504, 127.3845),
            //    new Missile("MSL-04", 35.9078, 127.7669),
            //};

            // 테스트용
            missiles = new List<Missile>
            {
                new Missile("MSL-01", (int)(37.5665 * 1e7), (int)(126.9780 * 1e7), (short)0, MissileState.LaunchReady),
                new Missile("MSL-02", (int)(36.3504 * 1e7), (int)(127.3845 * 1e7), (short)250, MissileState.InitialGuidance, "TGT-01"),
                new Missile("MSL-03", (int)(35.1796 * 1e7), (int)(129.0756 * 1e7), (short)480, MissileState.MidGuidance, "TGT-02"),
                new Missile("MSL-04", (int)(35.9078 * 1e7), (int)(127.7669 * 1e7), (short)720, MissileState.TerminalGuidance, "TGT-03"),
            };
        }

        // 미사일 검색
        // 리스트로 구현 시 인덱스 기반이 더 효율적이지만, 유도탄이 4개라 무시할만하고, ID기반이 관리가 편함
        public Missile? GetMissileById(string id)
        {
            return missiles.FirstOrDefault(m => m.Id == id);
        }

        public IEnumerable<Missile> GetAllMissiles()
            => missiles;

    }
}
