using C2.Services;
using C2.Views.Markers;
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
        private readonly MockMissileService _missileService;
        private readonly MockTargetService _targetService;
        //private readonly TargetService _targetService;
        private readonly MapService _mapService;

        private readonly GMapControl _map;
        private GMapPolygon _circle;

        private MissileMarkerViewModel? _focusedMissile;
        private TargetMarkerViewModel? _focusedTarget;
        private PIPMarkerViewModel? _focusedPip;

        public MapViewModel(GMapControl mapControl)
        {
            _map = mapControl;
            _mapService = MapService.Instance;
            _missileService = MockMissileService.Instance;
            _targetService = MockTargetService.Instance;

            InitializeMap();
            UpdateDispatcher.Instance.Register(UpdateMarkers);
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
            _circle = DrawDetectionCircle();
            _map.Markers.Add(_circle);
        }

        public void FocusMissileMarker(MissileMarkerViewModel missileVM)
        {

        }

        public void FocusTargetMarker(TargetMarkerViewModel targetVM)
        {

        }

        private void UpdateMarkers()
        {
            // 🔄 매 주기마다 지도 전체 마커 갱신
            _map.Markers.Clear();
            _map.Markers.Add(_circle);
            UpdateMissileMarker();
            UpdatePIPMarker();
            UpdateTargetMarker();
        }

        // 마커마다 디스패쳐와 연결해서 업데이트하는 방식 -> 뷰모델을 디스패쳐와 연결해서 전체 마커를 업데이트하는방식
        // 이유: 맵을 업데이트한다는것 -> 모든 마커들을 지우고 새로 그리는것
        // 각각의 마커 뷰모델에서 map에 등록된 마커 instance를 하나씩 추적해서 삭제하고 새로운 것을 추가하는것은 비효율적이기때문에
        // 맵 같은 경우는 뷰모델에서 전체 마커를 변경하는식으로 구현함

        private void UpdateMissileMarker()
        {
            foreach (var ctrl in _missileService.missileControllers)
            {
                var missile = ctrl.Missile;

                var vm = new MissileMarkerViewModel(missile);

                var marker = new GMapMarker(new PointLatLng(missile.Latitude, missile.Longitude))
                {
                    Shape = new MissileMarker { DataContext = vm },
                    Offset = new Point(-75, -30)
                };

                _map.Markers.Add(marker);
            }
        }
        private void UpdatePIPMarker()
        {
            foreach (var ctrl in _missileService.missileControllers)
            {
                var PIP = ctrl.PIP;
                if (PIP == null) continue;

                var vm = new PIPMarkerViewModel(PIP);

                var marker = new GMapMarker(new PointLatLng(PIP.Latitude, PIP.Longitude))
                {
                    Shape = new PIPMarker { DataContext = vm },
                    Offset = new Point(-75, -30)
                };

                _map.Markers.Add(marker);
            }
        }
        private void UpdateTargetMarker()
        {
            foreach (var ctrl in _targetService.TargetControllers)
            {
                var target = ctrl.Target;

                var vm = new TargetMarkerViewModel(target);

                var marker = new GMapMarker(new PointLatLng(target.CurLoc.Lat, target.CurLoc.Lon))
                {
                    Shape = new TargetMarker { DataContext = vm },
                    Offset = new Point(-75, -30)
                };

                _map.Markers.Add(marker);
            }
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

        ~MapViewModel()
        {
            UpdateDispatcher.Instance.Unregister(UpdateMarkers);
        }

    }
}
