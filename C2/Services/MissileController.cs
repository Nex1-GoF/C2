using C2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2.Services
{
    public class MissileController
    {
        private readonly Missile missile;

        public MissileController(Missile missile)
        {
            this.missile = missile;
        }

        public void Launch()
        {
            missile.State = MissileState.InitialGuidance;
            missile.FlightTime = 0;
            Console.WriteLine($"{missile.Id} 발사됨");
        }

        public void EmergencyExplode()
        {
            missile.State = MissileState.Abort;
            Console.WriteLine($"{missile.Id} 비상 폭파!");
        }

        public void AssignTarget(string targetId)
        {
            missile.TargetId = targetId;
            Console.WriteLine($"{missile.Id} → {targetId} 교전 할당됨");
        }

        public void Reset()
        {
            missile.State = MissileState.LaunchReady;
            missile.TargetId = null;
            missile.FlightTime = 0;
        }
    }

}
