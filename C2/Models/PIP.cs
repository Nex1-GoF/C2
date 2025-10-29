using CommunityToolkit.Mvvm.ComponentModel;
using GMap.NET;

namespace C2.Models
{
    public partial class PIP : ObservableObject
    {
        [ObservableProperty] private string _missileId;
        [ObservableProperty] private double _latitude;
        [ObservableProperty] private double _longitude;

        public PointLatLng Position => new PointLatLng(Latitude, Longitude);

        public PIP(string missileId, double latitude, double longitude)
        {
            _missileId = missileId;
            _latitude = latitude;
            _longitude = longitude;
        }

        public void UpdatePosition(double newLat, double newLon)
        {
            Latitude = newLat;
            Longitude = newLon;
        }
    }
}
