using C2.Services;
using C2.Views.Markers;
using CommunityToolkit.Mvvm.ComponentModel;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System;
using System.Collections.ObjectModel;
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
        private readonly List<MissileMarkerViewModel> _missileMarkers = new();
        private readonly List<TargetMarkerViewModel> _targetMarkers = new();
        private readonly List<PIPMarkerViewModel> _pipMarkers = new();

        private readonly MockMissileService _missileService;
        private readonly MockTargetService _targetService;

        private readonly MapService _mapService;

        private readonly GMapControl _map;
        private GMapPolygon _circle;

        private MissileMarkerViewModel? _focusedMissile;
        private TargetMarkerViewModel? _focusedTarget;
        private PIPMarkerViewModel? _focusedPip;
        private GMapRoute? _lineMissileToPip;
        private GMapRoute? _lineTargetToPip;
        private GMapRoute? _routeMissilePath;
        private GMapRoute? _routeTargetPath;

        public MapViewModel(GMapControl mapControl)
        {
            _map = mapControl;
            _mapService = MapService.Instance;
            _missileService = MockMissileService.Instance;
            _targetService = MockTargetService.Instance;
            _circle = DrawDetectionCircle();

            foreach (var ctrl in _missileService.missileControllers)
                _missileMarkers.Add(new MissileMarkerViewModel(ctrl.Missile));

            foreach (var ctrl in _targetService.TargetControllers)
                _targetMarkers.Add(new TargetMarkerViewModel(ctrl.Target));

            foreach (var pip in _missileService.PIPs)
                _pipMarkers.Add(new PIPMarkerViewModel(pip));

            InitializeMap();
            UpdateDispatcher.Instance.Register(UpdateMarkers);
        }

        private void InitializeMap()
        {
            _mapService.Initialize(_map);
            _map.Markers.Clear();
            _map.Markers.Add(_circle);
        }
        private void UpdateFocusPaths()
        {
            // 기존 경로 제거
            if (_routeMissilePath != null) _map.Markers.Remove(_routeMissilePath);
            if (_routeTargetPath != null) _map.Markers.Remove(_routeTargetPath);

            // 포커스가 없으면 종료
            if (_focusedMissile == null && _focusedTarget == null) return;

            // 1️⃣ 미사일 경로
            var missileCtrl = _missileService.missileControllers
                .FirstOrDefault(c => c.Missile.Id == _focusedMissile?.Id);
            if (missileCtrl?.PathHistory.Count > 1)
            {
                var missilePoints = missileCtrl.PathHistory
                    .Select(p => new PointLatLng(p.Lat, p.Lon))
                    .ToList();

                _routeMissilePath = CreatePathRoute(missilePoints, Colors.LightSkyBlue);
                _map.Markers.Add(_routeMissilePath);
            }

            // 2️⃣ 표적 경로
            var targetCtrl = _targetService.TargetControllers
                .FirstOrDefault(c => c.Target.Id.ToString() == _focusedTarget?.DefaultID);
            if (targetCtrl?.PathHistory.Count > 1)
            {
                var targetPoints = targetCtrl.PathHistory
                    .Select(p => new PointLatLng(p.Lat, p.Lon))
                    .ToList();

                _routeTargetPath = CreatePathRoute(targetPoints, Colors.OrangeRed);
                _map.Markers.Add(_routeTargetPath);
            }
        }

        private GMapRoute CreatePathRoute(List<PointLatLng> points, Color color)
        {
            return new GMapRoute(points)
            {
                Shape = new Path
                {
                    Stroke = new SolidColorBrush(Colors.Red),
                    StrokeThickness = 1.8,
                    Opacity = 0.8
                }
            };
        }
        public void FocusMissileMarker(MissileMarkerViewModel missileVM)
        {
            if (_focusedMissile == missileVM)
            {
                ClearFocus();
                return;
            }
            ClearFocus();

            _focusedMissile = missileVM;
            _focusedTarget = _targetMarkers
                .FirstOrDefault(vm => vm.DefaultID == missileVM.TargetId);
            _focusedPip = _pipMarkers
                .FirstOrDefault(vm => vm.MissileId == missileVM.Id);

            if (_focusedPip == null || _focusedTarget == null || _focusedMissile == null) return;

            missileVM.UpdateFocus(true);
            _focusedTarget?.UpdateFocus(true);
            _focusedPip?.UpdateVisible(true);
            UpdateFocusLines();
            UpdateFocusPaths();
        }


        public void FocusTargetMarker(TargetMarkerViewModel targetVM)
        {
            // 🔹 같은 Target 다시 클릭 시 포커스 해제
            if (_focusedTarget == targetVM)
            {
                ClearFocus();
                return;
            }
            // 🔹 기존 포커스 해제
            ClearFocus();

            // 🔹 새 포커스 지정
            _focusedTarget = targetVM;
            _focusedTarget.UpdateFocus(true);

            // 🔹 Target과 연결된 Missile 찾기 (TargetId 매칭)
            _focusedMissile = _missileMarkers
                .FirstOrDefault(vm => vm.TargetId == targetVM.DefaultID);
            
            if (_focusedMissile != null)
            {
                _focusedMissile.UpdateFocus(true);

                // 🔹 Missile에 연결된 PIP 찾기
                _focusedPip = _pipMarkers
                    .FirstOrDefault(vm => vm.MissileId == _focusedMissile.Id);

                _focusedPip?.UpdateVisible(true);
            }

            UpdateFocusLines();
            UpdateFocusPaths();
        }

        private void UpdateFocusLines()
        {
            if (_lineMissileToPip != null) _map.Markers.Remove(_lineMissileToPip);
            if (_lineTargetToPip != null) _map.Markers.Remove(_lineTargetToPip);

            // 포커스된 요소가 없으면 종료
            if (_focusedMissile == null || _focusedPip == null || _focusedTarget == null)
                return;

            // Missile ↔ PIP
            var missileToPipPoints = new List<PointLatLng>
            {
                new PointLatLng(_focusedMissile.Latitude, _focusedMissile.Longitude),
                new PointLatLng(_focusedPip.Latitude, _focusedPip.Longitude)
            };

            _lineMissileToPip = CreateDashedRoute(missileToPipPoints, Colors.LightSkyBlue);
            _map.Markers.Add(_lineMissileToPip);

            // Target ↔ PIP
            var targetToPipPoints = new List<PointLatLng>
            {
                new PointLatLng(_focusedTarget.Latitude, _focusedTarget.Longitude),
                new PointLatLng(_focusedPip.Latitude, _focusedPip.Longitude)
            };
            _lineTargetToPip = CreateDashedRoute(targetToPipPoints, Colors.OrangeRed);
            _map.Markers.Add(_lineTargetToPip);
        }

        private GMapRoute CreateDashedRoute(List<PointLatLng> points, Color color)
        {
            var route = new GMapRoute(points)
            {
                Shape = new Path
                {
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 3, 3 }, // 점선 패턴
                    Opacity = 0.8
                }
            };
            return route;
        }

        private void ClearFocus()
        {
            _focusedMissile?.UpdateFocus(false);
            _focusedTarget?.UpdateFocus(false);
            _focusedPip?.UpdateVisible(false);

            _focusedMissile = null;
            _focusedTarget = null;
            _focusedPip = null;

            if (_lineMissileToPip != null) _map.Markers.Remove(_lineMissileToPip);
            if (_lineTargetToPip != null) _map.Markers.Remove(_lineTargetToPip);
            if (_routeMissilePath != null) _map.Markers.Remove(_routeMissilePath); // ✅ 경로 제거
            if (_routeTargetPath != null) _map.Markers.Remove(_routeTargetPath);
        }
        private void UpdateMarkers()
        {
            // 🔄 매 주기마다 지도 전체 마커 갱신
            _map.Markers.Clear();
            _map.Markers.Add(_circle);
            UpdateMissileMarker();
            UpdatePIPMarker();
            UpdateTargetMarker();
            UpdateFocusLines();
            UpdateFocusPaths();
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
                var vm = _missileMarkers.FirstOrDefault(vm => vm.Id == missile.Id);

                if (vm == null)
                {
                    // 🟢 최초 생성 시만
                    vm = new MissileMarkerViewModel(missile);
                    _missileMarkers.Add(vm);
                }
                else
                {
                    // 🟡 이후에는 업데이트만
                    vm.UpdateMissileInfo(missile);
                }
                var marker = new GMapMarker(new PointLatLng(missile.Latitude, missile.Longitude))
                {
                    Shape = new MissileMarker { DataContext = vm },
                    Offset = new Point(-25, -25) // UserControl 중심 보정
                };
                _map.Markers.Add(marker);
            }
        }

        private void UpdatePIPMarker()
        {
            foreach (var ctrl in _missileService.missileControllers)
            {
                var pip = ctrl.PIP;
                if (pip == null) continue;

                var vm = _pipMarkers.FirstOrDefault(vm => vm.MissileId == pip.MissileId);

                if (vm == null)
                {
                    vm = new PIPMarkerViewModel(pip);
                    _pipMarkers.Add(vm);
                }
                else
                {
                    vm.UpdatePIP(pip);
                }

                var marker = new GMapMarker(new PointLatLng(pip.Latitude, pip.Longitude))
                {
                    Shape = new PIPMarker { DataContext = vm },
                    Offset = new Point(0,0) // UserControl 중심 보정
                };
                _map.Markers.Add(marker);
            }
        }

        private void UpdateTargetMarker()
        {
            foreach (var ctrl in _targetService.TargetControllers)
            {
                var target = ctrl.Target;
                var vm = _targetMarkers.FirstOrDefault(vm => vm.Id == $"TARGET-{target.Id:D3}");

                if (vm == null)
                {
                    vm = new TargetMarkerViewModel(target);
                    _targetMarkers.Add(vm);
                }
                else
                {
                    vm.UpdateTargetInfo(target);
                }
                var marker = new GMapMarker(new PointLatLng(target.CurLoc.Lat, target.CurLoc.Lon))
                {
                    Shape = new TargetMarker { DataContext = vm },
                    Offset = new Point(-25, -25) // UserControl 중심 보정
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
