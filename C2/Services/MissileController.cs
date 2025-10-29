using C2.Messages;
using C2.Models;
using CommunityToolkit.Mvvm.Messaging;
using GMap.NET;
using System.Threading.Tasks;

namespace C2.Services
{
    public class MissileController
    {
        private readonly Missile missile;
        private PIP? PIP;

        public MissileController(Missile missile)
        {
            this.missile = missile;
        }

        public void SetPIP(double Lat, double Lng)
        {
            if (PIP == null)
            {
                PIP = new PIP(missile.Id, Lat, Lng);
                return;
            }
            PIP.Latitude = Lat;
            PIP.Longitude = Lng;
        }

        public void SetPIP(PIP PIP)
        {
            this.PIP = PIP;
        }

        public async Task SimulateFlightAsync(int interval)
        {
            if (PIP == null) return;
            missile.State = MissileState.MidGuidance;

            double lat = missile.LatitudeRaw / 1e7;
            double lon = missile.LongitudeRaw / 1e7;

            for (int i = 0; i < 300; i++)
            {
                await Task.Delay(interval);

                
                double targetLat = PIP.Latitude;
                double targetLon = PIP.Longitude;

                lat += (targetLat - lat) * 0.01;
                lon += (targetLon - lon) * 0.01;

                missile.LatitudeRaw = (int)(lat * 1e7);
                missile.LongitudeRaw = (int)(lon * 1e7);

                // UI 갱신용 메시지
                WeakReferenceMessenger.Default.Send(
                    new MissileUpdateMessage(new MissileUpdateData(missile.Id, lat, lon))
                );
            }

            missile.State = MissileState.TerminalGuidance;
        }
    }
}
