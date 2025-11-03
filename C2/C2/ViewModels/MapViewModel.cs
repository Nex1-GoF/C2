using C2.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace C2.ViewModels
{

    public partial class MapViewModel : ObservableObject
    {
        //private readonly MissileService _missileService;
        //private readonly TargetService _targetService;
        private readonly MapService _mapService;

        private readonly GMapControl _map;

        public MapViewModel(GMapControl mapControl)
        {
            _map = mapControl;
            _mapService = MapService.Instance;
            //_missileService = MissileService.Instance;
            //_targetService = TargetService.Instance;

            InitializeMap();
        }

        private void InitializeMap()
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            _map.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            _map.MinZoom = 2;
            _map.MaxZoom = 18;
            _map.Zoom = 7;
            _map.Position = new PointLatLng(36.5, 127.5);
            _map.CanDragMap = true;
            _map.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            _map.IgnoreMarkerOnMouseWheel = true; // 마커 위에서도 휠 줌 동작
            _map.MouseWheelZoomEnabled = true;    // 마우스 휠로 줌 가능

            _map.Markers.Clear();

            _map.Markers.Add(DrawDetectionCircle());
        }

        private GMapPolygon DrawDetectionCircle()
        {
            if (_map == null || _mapService.Distance <= 0)
            {
                throw new Exception("예외 발생: 맵이 로드되지 않았거나, 원을 그릴 수 없습니다.");
            }

            var points = CreateCircle(_mapService.Center, _mapService.Distance, 72);
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
