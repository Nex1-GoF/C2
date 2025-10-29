using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace C2.Views
{
    public partial class MapPanel : UserControl
    {
        public MapPanel()
        {
            InitializeComponent();
            Loaded += (s, e) => OnLoaded(s, e);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            PART_Map.MapProvider = OpenStreetMapProvider.Instance;
            PART_Map.MinZoom = 2;
            PART_Map.MaxZoom = 18;
            PART_Map.Zoom = 8;

            // 서울 좌표
            var center = new PointLatLng(38.5665, 126.9780);
            var Seoul = new PointLatLng(37.5665, 126.9780);
            PART_Map.Position = center;

            // 반경 350km 탐지 원
            // 350km은 지도에 안보여서 임시적으로 250km으로 수정
            var circlePoints = CreateCircle(Seoul, 250_000, 72);
            var circle = new GMapPolygon(circlePoints)
            {
                Shape = new System.Windows.Shapes.Path
                {
                    Stroke = Brushes.LimeGreen,             // 초록색 윤곽선
                    StrokeThickness = 2,                    // 선 두께
                    Fill = Brushes.Transparent,             // 내부 비움
                    
                }
            };
            
            PART_Map.Markers.Add(circle);
        }

        private static List<PointLatLng> CreateCircle(PointLatLng center, double radiusMeters, int segments)
        {
            var points = new List<PointLatLng>();
            const double EarthRadius = 6378137.0;

            double lat = ToRadians(center.Lat);
            double lon = ToRadians(center.Lng);
            double d = radiusMeters / EarthRadius;

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double latPoint = Math.Asin(Math.Sin(lat) * Math.Cos(d) + Math.Cos(lat) * Math.Sin(d) * Math.Cos(angle));
                double lonPoint = lon + Math.Atan2(Math.Sin(angle) * Math.Sin(d) * Math.Cos(lat), Math.Cos(d) - Math.Sin(lat) * Math.Sin(latPoint));
                points.Add(new PointLatLng(ToDegrees(latPoint), ToDegrees(lonPoint)));
            }
            return points;
        }

        private static double ToRadians(double deg) => deg * Math.PI / 180.0;
        private static double ToDegrees(double rad) => rad * 180.0 / Math.PI;
    }
}
