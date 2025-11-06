using C2.Models;
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

        private readonly TargetViewModel _targetViewModel; // 업데이트
        private readonly MissilePanelViewModel _missileViewModel; // 업데이트

        //private readonly MockMissileService _missileService;
        //private readonly MockTargetService _targetService;
        private readonly MissileService _missileService;
        private readonly TargetService _targetService;

        private readonly MapService _mapService;

        private readonly GMapControl _map;
        private GMapPolygon _circle;

        private MissileMarkerViewModel? _selectedMissileMarkerVM;
        private TargetMarkerViewModel? _selectedTargetMarkerVM;
        private PIPMarkerViewModel? _selectedPIPMarkerVM;
        private GMapRoute? _lineMissileToPip;
        private GMapRoute? _lineTargetToPip;
        private GMapRoute? _routeMissilePath;
        private GMapRoute? _routeTargetPath;

        public MapViewModel(GMapControl mapControl)
        {
            _map = mapControl;
            _mapService = MapService.Instance;
            _missileService = MissileService.Instance;
            _targetService = TargetService.Instance;
            _circle = DrawDetectionCircle();

            foreach (var msl in _missileService.GetAllMissiles())
            {
                _missileMarkers.Add(new MissileMarkerViewModel(msl));
                if (msl.PIP == null) continue;

                _pipMarkers.Add(new PIPMarkerViewModel(msl.PIP, msl.Id));
            }

            foreach (var tgt in _targetService.GetAllTargets())
                _targetMarkers.Add(new TargetMarkerViewModel(tgt));

           _targetViewModel = TargetViewModel.Instance;
            _missileViewModel = MissilePanelViewModel.Instance;
                

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
            if (_missileService.SelectedMissile == null && _targetService.SelectedTarget == null) return;

            // 1️ 미사일 경로
            var missile = _missileService.GetAllMissiles()
                .FirstOrDefault(c => c.Id == _missileService.SelectedMissile?.Id);
            if (missile?.PathHistory.Count > 1)
            {
                var missilePoints = missile.PathHistory
                    .Select(p => new PointLatLng(p.Lat, p.Lon))
                    .ToList();

                _routeMissilePath = CreatePathRoute(missilePoints, Colors.LightSkyBlue);
                _map.Markers.Add(_routeMissilePath);
            }

            // 2️⃣ 표적 경로
            var target = _targetService.GetAllTargets()
                .FirstOrDefault(c => c.Id == _targetService.SelectedTarget?.Id);
            if (target?.PathHistory.Count > 1)
            {
                var targetPoints = target.PathHistory
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
            if (_selectedMissileMarkerVM == missileVM)
            {
                ClearFocus();
                return;
            }
            ClearFocus();

            var id = missileVM.Id;
            var missile = _missileService.GetMissile(id);

            if (missile == null) return; // 만약 미사일이 없어시면 종료

            _missileService.SelectMissile(id);

            if (missile.TargetId != null)
            {
                _targetService.SelectTarget(missile.TargetId[0]);
            }
        }


        public void FocusTargetMarker(TargetMarkerViewModel targetVM)
        {
            // 🔹 같은 Target 다시 클릭 시 포커스 해제
            if (_selectedTargetMarkerVM == targetVM)
            {
                ClearFocus();
                return;
            }
            // 🔹 기존 포커스 해제
            ClearFocus();

            // 🔹 새 포커스 지정
            string targetId = targetVM.DefaultID;
            var target= targetVM.Target;

            _targetViewModel.SelectTarget(target);
            //_targetService.SelectTarget(target); // Todo: 서비스로 동작하고 메세지로 받도록

            var missile = _missileService.GetAllMissiles()
                .FirstOrDefault(m=>m.TargetId != null && m.TargetId == targetId);

            if (missile != null) {
                //_missileService.SelectMissile(missile.Id);
                _missileViewModel.SelectMissile(missile);
            }
        }

        private void UpdateFocusLines()
        {
            if (_lineMissileToPip != null) _map.Markers.Remove(_lineMissileToPip);
            if (_lineTargetToPip != null) _map.Markers.Remove(_lineTargetToPip);

            // 포커스된 요소가 없으면 종료
            if (_selectedMissileMarkerVM == null || _selectedPIPMarkerVM == null || _selectedTargetMarkerVM == null)
                return;

            // Missile ↔ PIP
            var missileToPipPoints = new List<PointLatLng>
            {
                new PointLatLng(_selectedMissileMarkerVM.Latitude, _selectedMissileMarkerVM.Longitude),
                new PointLatLng(_selectedPIPMarkerVM.Latitude, _selectedPIPMarkerVM.Longitude)
            };

            _lineMissileToPip = CreateDashedRoute(missileToPipPoints, Colors.LightSkyBlue);
            _map.Markers.Add(_lineMissileToPip);

            // Target ↔ PIP
            var targetToPipPoints = new List<PointLatLng>
            {
                new PointLatLng(_selectedTargetMarkerVM.Latitude, _selectedTargetMarkerVM.Longitude),
                new PointLatLng(_selectedPIPMarkerVM.Latitude, _selectedPIPMarkerVM.Longitude)
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

            _missileService.ClearMissile();
            _targetService.ClearTarget();

            if (_selectedMissileMarkerVM != null) _selectedMissileMarkerVM.UpdateFocus(false);
            if (_selectedTargetMarkerVM != null) _selectedTargetMarkerVM.UpdateFocus(false);
            if (_selectedPIPMarkerVM != null) _selectedPIPMarkerVM.UpdateVisible(false);

            _selectedMissileMarkerVM = null;
            _selectedTargetMarkerVM = null;
            _selectedPIPMarkerVM = null;

            if (_lineMissileToPip != null) _map.Markers.Remove(_lineMissileToPip);
            if (_lineTargetToPip != null) _map.Markers.Remove(_lineTargetToPip);
            if (_routeMissilePath != null) _map.Markers.Remove(_routeMissilePath); // ✅ 경로 제거
            if (_routeTargetPath != null) _map.Markers.Remove(_routeTargetPath);
        }

        private void UpdateFocus()
        {
            if(_missileService.SelectedMissile != null)
            {
                var selectedMissile = _missileService.SelectedMissile;
                var missileVM = _missileMarkers
                    .FirstOrDefault(m => m.Id == selectedMissile.Id);

                if (missileVM != null)
                {

                    if (_selectedMissileMarkerVM != null)
                    {
                        _selectedMissileMarkerVM.UpdateFocus(false);
                        _selectedMissileMarkerVM = null;
                    }

                    _selectedMissileMarkerVM = missileVM;
                    _selectedMissileMarkerVM.UpdateFocus(true);
                }

                var selectedPIP = selectedMissile.PIP;

                if (selectedPIP != null)
                {
                    var PIPVm = _pipMarkers.FirstOrDefault(m => m.MissileId == selectedMissile.Id);
                    if(PIPVm != null)
                    {
                        _selectedPIPMarkerVM = PIPVm;
                        _selectedPIPMarkerVM.UpdateVisible(true);
                    }
                }

            }
            if(_targetService.SelectedTarget != null)
            {
                var selectedTarget = _targetService.SelectedTarget;
                var targetVm = _targetMarkers
                    .FirstOrDefault(t => t.DefaultID == selectedTarget.Id.ToString());

                if (targetVm != null) { 

                    if(_selectedTargetMarkerVM != null)
                    {
                        _selectedTargetMarkerVM.UpdateFocus(false);
                        _selectedTargetMarkerVM = null;
                    }


                    _selectedTargetMarkerVM = targetVm;
                    _selectedTargetMarkerVM.UpdateFocus(true);
                }
            }


            if(_targetService.SelectedTarget == null && _selectedTargetMarkerVM != null)
            {
                _selectedTargetMarkerVM.UpdateFocus(false);
                _selectedTargetMarkerVM = null;
            }

        }

        private void UpdateMarkers()
        {
            // 🔄 매 주기마다 지도 전체 마커 갱신
            _map.Markers.Clear();
            _map.Markers.Add(_circle);
            UpdateMissileMarker();
            UpdatePIPMarker();
            UpdateTargetMarker();
            UpdateFocus();
            UpdateFocusLines();
            UpdateFocusPaths();
        }

        // 마커마다 디스패쳐와 연결해서 업데이트하는 방식 -> 뷰모델을 디스패쳐와 연결해서 전체 마커를 업데이트하는방식
        // 이유: 맵을 업데이트한다는것 -> 모든 마커들을 지우고 새로 그리는것
        // 각각의 마커 뷰모델에서 map에 등록된 마커 instance를 하나씩 추적해서 삭제하고 새로운 것을 추가하는것은 비효율적이기때문에
        // 맵 같은 경우는 뷰모델에서 전체 마커를 변경하는식으로 구현함

        private void UpdateMissileMarker()
        {
            foreach (var msl in _missileService.GetAllMissiles())
            {

                var vm = _missileMarkers.FirstOrDefault(vm => vm.Id == msl.Id);

                if (vm == null)
                {
                    // 🟢 최초 생성 시만
                    vm = new MissileMarkerViewModel(msl);
                    _missileMarkers.Add(vm);
                }
                else
                {
                    // 🟡 이후에는 업데이트만
                    vm.UpdateMissileInfo(msl);
                }
                var marker = new GMapMarker(new PointLatLng(msl.Latitude, msl.Longitude))
                {
                    Shape = new MissileMarker { DataContext = vm },
                    Offset = new Point(-25, -25) // UserControl 중심 보정
                };
                _map.Markers.Add(marker);
            }
        }

        private void UpdatePIPMarker()
        {
            foreach (var msl in _missileService.GetAllMissiles())
            {
                var pip = msl.PIP;
                if (pip == null) continue;

                var vm = _pipMarkers.FirstOrDefault(vm => vm.MissileId == msl.Id);

                if (vm == null)
                {

                    vm = new PIPMarkerViewModel(pip, msl.Id);
                    _pipMarkers.Add(vm);

                }
                else
                {
                    //vm.UpdatePIP(pip);
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
            foreach (var tgt in _targetService.GetAllTargets())
            {
                var vm = _targetMarkers.FirstOrDefault(vm => vm.Id == $"TARGET-{tgt.Id:D3}");

                if (vm == null)
                {
                    vm = new TargetMarkerViewModel(tgt);
                    _targetMarkers.Add(vm);
                }
                else
                {
                    vm.UpdateTargetInfo(tgt);
                }
                var marker = new GMapMarker(new PointLatLng(tgt.CurLoc.Lat, tgt.CurLoc.Lon))
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
