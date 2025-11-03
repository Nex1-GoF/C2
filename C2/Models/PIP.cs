using CommunityToolkit.Mvvm.ComponentModel;
using GMap.NET;

namespace C2.Models
{
    public partial class PIP
    {
        public string MissileId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public PointLatLng Position => new PointLatLng(Latitude, Longitude);

        public PIP(string missileId, double latitude, double longitude)
        {
            MissileId = missileId;
            Latitude = latitude;
            Longitude = longitude;
        }

        public void UpdatePosition(double newLat, double newLon)
        {
            Latitude = newLat;
            Longitude = newLon;
        }
    }
}
