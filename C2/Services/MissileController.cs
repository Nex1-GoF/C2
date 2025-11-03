using C2.Models;
using CommunityToolkit.Mvvm.Messaging;
using GMap.NET;
using System.Threading.Tasks;

namespace C2.Services
{
    public class MissileController
    {
        public  Missile Missile {  get; }
        public PIP? PIP { get; set; }

        public MissileController(Missile missile)
        {
            this.Missile = missile;
        }


        public async Task SimulateFlightAsync(int interval)
        {
            if (PIP == null) return;
            Missile.State = MissileState.MidGuidance;

            double lat = Missile.LatitudeRaw / 1e7;
            double lon = Missile.LongitudeRaw / 1e7;

            for (int i = 0; i < 300; i++)
            {
                await Task.Delay(interval);

                
                double targetLat = PIP.Latitude;
                double targetLon = PIP.Longitude;

                lat += (targetLat - lat) * 0.01;
                lon += (targetLon - lon) * 0.01;

                Missile.LatitudeRaw = (int)(lat * 1e7);
                Missile.LongitudeRaw = (int)(lon * 1e7);

            }

            Missile.State = MissileState.TerminalGuidance;
        }
    }
}
