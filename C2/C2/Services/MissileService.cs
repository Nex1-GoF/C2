using C2.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C2.Services
{
    public class MissileService
    {
        // 📍 발사대 위치 (서울 시청 인근)
        private readonly (int latitude, int longitude, short altitude) C2Points =
            ((int)(37.5665 * 1e7), (int)(126.9780 * 1e7), (short)(38));

        private static MissileService _instance;
        public static MissileService Instance => _instance ??= new MissileService();

        // ✅ Dictionary로 변경 (Key: Missile ID)
        private readonly Dictionary<string, Missile> _missiles = new();

        // ✅ 선택된 미사일 (단일)
        public Missile? SelectedMissile { get; private set; }

        private MissileService()
        {
            // 초기 미사일 4기 등록
            for (int i = 1; i <= 4; i++)
            {
                var missile = new Missile(
                    id: $"MSL-{i:00}",
                    latitudeRaw: C2Points.latitude,
                    longitudeRaw: C2Points.longitude,
                    altitude: C2Points.altitude
                );

                _missiles[missile.Id] = missile;
            }
        }

        // ✅ 전체 미사일 반환 (읽기 전용 Dictionary)
        public IReadOnlyDictionary<string, Missile> GetAllMissiles() => _missiles;

        // ✅ 특정 미사일 직접 가져오기
        public Missile? GetMissile(string id)
        {
            _missiles.TryGetValue(id, out var missile);
            return missile;
        }

        // ✅ 교전할당 시 사용
        //public bool TryAssignMissile()
        //{

            
        //}

        // ✅ 미사일 선택
        public bool SelectMissile(string id)
        {
            if (!_missiles.TryGetValue(id, out var missile))
                return false;

            SelectedMissile = missile;
            return true;
        }

        // ✅ 선택 해제
        public void ClearMissile()
        {
            SelectedMissile = null;
        }
    }
}
