using GMap.NET;
using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace C2.Services
{
    public class MapService
    {
        private static MapService _instance;
        public static MapService Instance => _instance ??= new MapService();

        private GMapControl? _map;
        public GMapControl? Map => _map;

        public PointLatLng Center { get; set; } = new PointLatLng(37.5665, 126.9780);
        public double Distance { get; set; } = 250_000;

        // 🔹 실제 지도에 표시될 마커/경로 컬렉션

        private MapService() { }


        public void Initialize(GMapControl map)
        {
            // 맵 초기화
            _map = map;
            _map.MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance;
            _map.MinZoom = 2;
            _map.MaxZoom = 18;
            _map.Zoom = 8;
            _map.Position = Center;
            _map.CanDragMap = true;

            // 원 그리기
        }

        public GMapPolygon? DrawDetectionCircle()
        {
            if (_map == null || Distance <= 0) return null;

            var points = CreateCircle(Center, Distance, 72);
            GMapPolygon CirclePolygon = new GMapPolygon(points)
            {
                Shape = new Path
                {
                    Stroke = Brushes.LimeGreen,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                }
            };
            return CirclePolygon;
        }



        private static List<PointLatLng> CreateCircle(PointLatLng center, double radiusMeters, int segments)
        {
            const double EarthRadius = 6378137.0;
            var points = new List<PointLatLng>();
            double lat = ToRadians(center.Lat);
            double lon = ToRadians(center.Lng);
            double d = radiusMeters / EarthRadius;

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double latPoint = Math.Asin(Math.Sin(lat) * Math.Cos(d) +
                                            Math.Cos(lat) * Math.Sin(d) * Math.Cos(angle));
                double lonPoint = lon + Math.Atan2(Math.Sin(angle) * Math.Sin(d) * Math.Cos(lat),
                                                   Math.Cos(d) - Math.Sin(lat) * Math.Sin(latPoint));
                points.Add(new PointLatLng(ToDegrees(latPoint), ToDegrees(lonPoint)));
            }
            return points;
        }

        private static double ToRadians(double deg) => deg * Math.PI / 180.0;
        private static double ToDegrees(double rad) => rad * 180.0 / Math.PI;
    }
}
