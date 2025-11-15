using C2.Messages;
using C2.Models;
using C2.Services;
using C2.Views.Markers;
using CommunityToolkit.Mvvm.Messaging;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace C2.ViewModels
{
    public partial class MapViewModel
    {
        private readonly MapService _mapService = MapService.Instance;
        private readonly GMapControl _map;

        private GMapPolygon? _circle;

        // MarkerVM 캐시 (뷰모델에서만 관리)
        private readonly Dictionary<string, MissileMarkerViewModel> _missileVMs = new();
        private readonly Dictionary<char, TargetMarkerViewModel> _targetVMs = new();
        private readonly Dictionary<string, PIPMarkerViewModel> _pipVMs = new();

        public MapViewModel(GMapControl mapControl)
        {
            _map = mapControl;

            // 지도 초기화 (뷰모델 책임)
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            _map.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            _map.MinZoom = 2;
            _map.MaxZoom = 18;
            _map.Zoom = 7;
            _map.Position = new PointLatLng(36.5, 127.5);
            _map.CanDragMap = true;
            _map.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            _map.IgnoreMarkerOnMouseWheel = true;
            _map.MouseWheelZoomEnabled = true;

            // 탐지 원
            _circle = DrawDetectionCircle(_mapService.Center, _mapService.Distance);
            _map.Markers.Add(_circle);

            // 이벤트 구독 (서비스가 스냅샷 갱신을 알려줌)
            _mapService.SnapshotUpdated += RedrawAll;

            // 주기 갱신 등록
            UpdateDispatcher.Instance.Register(_mapService.Tick);

            WeakReferenceMessenger.Default.Register<MissileLaunchMessage>(this, (r,msg) => {
                string missileId = msg.Value;
                _missileVMs[missileId].UpdateVisible(true);
            });
        }

        // 뷰에서 클릭 시 호출
        public void OnMissileClicked(string missileId) => _mapService.OnMissileClicked(missileId);
        public void OnTargetClicked(char targetId) => _mapService.OnTargetClicked(targetId);

        ~MapViewModel()
        {
            _mapService.SnapshotUpdated -= RedrawAll;
            UpdateDispatcher.Instance.Unregister(_mapService.Tick);
        }

        // ======================
        // Draw (스펙을 그리기만)
        // ======================
        private void RedrawAll()
        {
            _map.Markers.Clear();
            if (_circle != null) _map.Markers.Add(_circle);

            // 1) 마커
            foreach (var mk in _mapService.GetMarkerSpecs())
            {
                FrameworkElement shape;

                if (mk.Kind == "Missile")
                {
                    if (!_missileVMs.TryGetValue(mk.Id, out var vm))
                    {
                        var m = _mapService.GetMissiles().First(x => x.Id == mk.Id);
                        _missileVMs[mk.Id] = vm = new MissileMarkerViewModel(m);
                    }
                    vm.UpdateFocus(mk.Focused);
                    vm.UpdateMissileInfo();
                    shape = new MissileMarker { DataContext = vm };
                }
                else if (mk.Kind == "Target")
                {
                    char tid = mk.Id[0];
                    if (!_targetVMs.TryGetValue(tid, out var vm))
                    {
                        var t = _mapService.GetTargets().First(x => x.Id == tid);
                        _targetVMs[tid] = vm = new TargetMarkerViewModel(t);
                    }
                    vm.UpdateFocus(mk.Focused);
                    vm.UpdateTargetInfo();
                    shape = new TargetMarker { DataContext = vm };
                }
                else // "PIP"
                {
                    var missileId = mk.Id.Replace("PIP::", "");
                    if (!_pipVMs.TryGetValue(missileId, out var vm))
                    {
                        var m = _mapService.GetMissiles().First(x => x.Id == missileId);
                        if (m.PIP == null) continue;
                        _pipVMs[missileId] = vm = new PIPMarkerViewModel(m.PIP, missileId);
                    }
                    vm.UpdateVisible(mk.Visible);
                    vm.UpdatePIP();
                    shape = new PIPMarker { DataContext = vm };
                }

                var marker = new GMapMarker(new PointLatLng(mk.Lat, mk.Lon))
                {
                    Shape = shape,
                    Offset = (mk.Kind == "PIP") ? new Point(-10,-10) : (mk.Kind == "Target") ?  new Point(-10, -30) : new Point(-10,-10)
                };
                _map.Markers.Add(marker);
            }

            // 2) 라인
            foreach (var ln in _mapService.GetLineSpecs())
            {
                var pts = new List<PointLatLng> { ln.From, ln.To };
                _map.Markers.Add(CreateDashed(pts)); // "DashedBlack" 스타일만 사용
            }

            // 3) 경로
            foreach (var rt in _mapService.GetRouteSpecs())
            {
                _map.Markers.Add(CreatePath(rt.Points, rt.Color));
            }
        }

        // ======================
        // Helpers (뷰모델 전용)
        // ======================
        private GMapPolygon DrawDetectionCircle(PointLatLng center, double radiusMeters)
        {
            const double R = 6378137.0;
            var pts = new List<PointLatLng>();
            double lat = Deg2Rad(center.Lat);
            double lon = Deg2Rad(center.Lng);
            double d = radiusMeters / R;

            for (int i = 0; i <= 72; i++)
            {
                double a = 2 * System.Math.PI * i / 72;
                double latP = System.Math.Asin(System.Math.Sin(lat) * System.Math.Cos(d) +
                                               System.Math.Cos(lat) * System.Math.Sin(d) * System.Math.Cos(a));
                double lonP = lon + System.Math.Atan2(System.Math.Sin(a) * System.Math.Sin(d) * System.Math.Cos(lat),
                                                      System.Math.Cos(d) - System.Math.Sin(lat) * System.Math.Sin(latP));
                pts.Add(new PointLatLng(Rad2Deg(latP), Rad2Deg(lonP)));
            }

            return new GMapPolygon(pts)
            {
                Shape = new System.Windows.Shapes.Path
                {
                    Stroke = Brushes.LimeGreen,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                }
            };
        }

        private static double Deg2Rad(double d) => d * System.Math.PI / 180.0;
        private static double Rad2Deg(double r) => r * 180.0 / System.Math.PI;

        private static GMapRoute CreateDashed(List<PointLatLng> pts) => new(pts)
        {
            Shape = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 3, 3 },
                Opacity = 0.8
            }
        };

        private static GMapRoute CreatePath(List<PointLatLng> pts, Color color) => new(pts)
        {
            Shape = new System.Windows.Shapes.Path
            {
                Stroke = new SolidColorBrush(color),
                StrokeThickness = 1.8,
                Opacity = 0.8
            }
        };
    }
}
