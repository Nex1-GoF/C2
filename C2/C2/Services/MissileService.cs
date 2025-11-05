using C2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace C2.Services
{
    class MissileService
    {

        (int latitude, int longitude, short artitude) C2Points = ((int)(37.5665 * 1e7), (int)(126.9780*1e7), (short)(38));

        private static MissileService _instance;
        public static MissileService Instance => _instance ??= new MissileService();

        private List<Missile> _missiles;

        private MissileService()
        {
            _missiles = new List<Missile>();    
            for(int i = 1; i <= 4; i++)
            {
                _missiles.Add(new Missile(
                    id: $"MSL-{i + 1:00}",
                    latitudeRaw: C2Points.latitude,
                    longitudeRaw: C2Points.longitude,
                    altitude: C2Points.artitude
                    ));
            }

        }
        public List<Missile> getAllMissiles()
        {
            return _missiles;
        }

        public bool TryAssignMissile(string targetId)
        {
            Missile? missile = _missiles.FirstOrDefault(m=> m.State == MissileState.LaunchReady && m.TargetId != null);

            if (missile == null) {
                return false;
            }

            missile.TargetId = targetId;
            return true;

        }





    }


}
