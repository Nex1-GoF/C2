using C2.Messages;
using C2.Models;
using C2.Services;
using C2.Views.Markers;
using CommunityToolkit.Mvvm.Messaging;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using System.Collections.Generic;
using System.Diagnostics;
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

        private GMapPolygon _circle;  // 탐지 원 (고정)

        // ================================
        // 관리 컬렉션
        // ================================
        private readonly List<MissileMarkerViewModel> mslVMs = new();
        private readonly List<TargetMarkerViewModel> tgtVMs = new();
        private readonly List<PIPMarkerViewModel> pipVMs = new();

        private readonly Dictionary<string, List<GMapRoute>> msl_paths = new();
        private readonly Dictionary<string, List<GMapRoute>> tgt_paths = new();

        // 교전 관계
        private readonly Dictionary<string, MissileEngagement> msl_set = new();
        private readonly Dictionary<string, TargetEngagement> tgt_set = new();

        public MapViewModel(GMapControl mapControl)
        {
            _map = mapControl;

            // 지도 초기화
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            _map.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            _map.MinZoom = 2;
            _map.MaxZoom = 18;
            _map.Zoom = 7;
            _map.Position = new PointLatLng(36.5, 127.5);
            _map.CanDragMap = true;
            _map.MouseWheelZoomType = MouseWheelZoomType.MousePositionAndCenter;
            _map.IgnoreMarkerOnMouseWheel = true;
            _map.MouseWheelZoomEnabled = true;

            // 탐지 원
            _circle = DrawDetectionCircle(_mapService.Center, _mapService.Distance);
            _map.Markers.Add(_circle);

            // 주기 갱신
            UpdateDispatcher.Instance.Register(UpdatePosition);

            // ================================
            // 메시지 수신 등록
            // ================================

            // 1️⃣ 미사일 선택 메시지
            WeakReferenceMessenger.Default.Register<MissileSelectedMessage>(this, (r, m) =>
            {
                string? id = m.Value;

                if (id == null)
                {
                    // 전체 해제
                    foreach (var list in msl_paths.Values)
                        foreach (var seg in list)
                            seg.Shape.Visibility = Visibility.Hidden;

                    foreach (var vm in mslVMs)
                    {
                        vm.UpdateFocus(false);
                        var pipVm = pipVMs.FirstOrDefault(pip => pip.MissileId == vm.Id);
                        if (pipVm != null) pipVm.UpdateVisible(false);
                    }
                    return;
                }

                // 특정 미사일만 강조
                foreach (var vm in mslVMs)
                {
                    bool isSelected = vm.Id == id;
                    vm.UpdateFocus(isSelected);

                    var pipVm = pipVMs.FirstOrDefault(pip => pip.MissileId == vm.Id);
                    if (pipVm != null) pipVm.UpdateVisible(isSelected);

                    // 경로 표시
                    if (msl_paths.TryGetValue(vm.Id, out var list))
                    {
                        foreach (var seg in list)
                            seg.Shape.Visibility = isSelected ? Visibility.Visible : Visibility.Hidden;
                    }
                }
            });

            // 2️⃣ 교전 할당 메시지
            WeakReferenceMessenger.Default.Register<EngagementAssignedMessage>(this, (r, m) =>
            {
                string missileId = m.Value.MissileId;
                string targetId = m.Value.TargetId;

                var mslVM = mslVMs.FirstOrDefault(v => v.Id == missileId);
                var tgtVM = tgtVMs.FirstOrDefault(v => v.DefaultID.ToString() == targetId);

                if (mslVM == null || tgtVM == null)
                    return;

                // msl_set 등록
                if (!msl_set.ContainsKey(missileId))
                    msl_set[missileId] = new MissileEngagement(mslVM);

                // tgt_set 등록
                if (!tgt_set.ContainsKey(targetId))
                    tgt_set[targetId] = new TargetEngagement(tgtVM);

                msl_set[missileId].MslVM = mslVM;
                msl_set[missileId].TgtVM = tgtVM;
                tgt_set[targetId].TgtVM = tgtVM;
                tgt_set[targetId].MslVMs.Add(mslVM);
            });

            // 3️⃣ PIP 계산 메시지
            WeakReferenceMessenger.Default.Register<PipCalculatedMessage>(this, (r, m) =>
            {
                string missileId = m.Value.MissileId;
                double pipLat = m.Value.Lat;
                double pipLon = m.Value.Lon;
                short pipAlt = m.Value.Alt;
                

                var mslVM = mslVMs.FirstOrDefault(v => v.Id == missileId);
                if (mslVM == null) return;

                var missile = _mapService.GetMissiles().FirstOrDefault(x => x.Id == missileId);
                if (missile == null || string.IsNullOrEmpty(missile.TargetId)) return;

                string targetId = missile.TargetId;
                var tgtVM = tgtVMs.FirstOrDefault(v => v.DefaultID == targetId[0]);
                if (tgtVM == null) return;

                // PIP VM 생성 또는 갱신
                var pipVM = pipVMs.FirstOrDefault(p => p.MissileId == missileId);
                if (pipVM == null)
                {
                    pipVM = new PIPMarkerViewModel(new PIP(pipLat, pipLon, pipAlt), missileId);
                    pipVMs.Add(pipVM);
                }
                else
                {
                    pipVM.UpdatePIP();
                }

                // PIP 마커 추가
                var pipMarker = new GMapMarker(new PointLatLng(pipLat, pipLon))
                {
                    Shape = new PIPMarker { DataContext = pipVM }
                };
                _map.Markers.Add(pipMarker);

                // 점선 생성 (미사일→PIP, PIP→타겟)
                PointLatLng fromMsl = new PointLatLng(mslVM.Latitude, mslVM.Longitude);
                PointLatLng pip = new PointLatLng(pipLat, pipLon);
                PointLatLng toTgt = new PointLatLng(tgtVM.Latitude, tgtVM.Longitude);

                var routeMslToPip = CreateDashedRoute(fromMsl, pip, Colors.SkyBlue);
                var routePipToTgt = CreateDashedRoute(pip, toTgt, Colors.Yellow);
                _map.Markers.Add(routeMslToPip);
                _map.Markers.Add(routePipToTgt);

                // msl_set 갱신
                if (!msl_set.ContainsKey(missileId))
                    msl_set[missileId] = new MissileEngagement(mslVM);

                var mEntry = msl_set[missileId];
                mEntry.PipVM = pipVM;
                mEntry.TgtVM = tgtVM;
                mEntry.Routes.Add(routeMslToPip);
                mEntry.Routes.Add(routePipToTgt);

                // tgt_set 갱신
                if (!tgt_set.ContainsKey(targetId))
                    tgt_set[targetId] = new TargetEngagement(tgtVM);

                var tEntry = tgt_set[targetId];
                if (!tEntry.PipVMs.Contains(pipVM))
                    tEntry.PipVMs.Add(pipVM);
                if (!tEntry.MslVMs.Contains(mslVM))
                    tEntry.MslVMs.Add(mslVM);

                tEntry.Routes.Add(missileId, new List<GMapRoute>());
                tEntry.Routes[missileId].Add(routeMslToPip);
                tEntry.Routes[missileId].Add(routePipToTgt);
                

            });
        }

        // ======================
        // Update
        // ======================
        public void UpdatePosition()
        {
            if (!_map.Markers.Contains(_circle))
                _map.Markers.Insert(0, _circle);

            foreach (var vm in pipVMs)
                vm.UpdatePIP();
            // 미사일 위치
            foreach (var vm in mslVMs)
            {
                PointLatLng prev = new PointLatLng(vm.Latitude, vm.Longitude);
                vm.UpdateMissileInfo();
                PointLatLng next = new PointLatLng(vm.Latitude, vm.Longitude);

                if (prev.Lat != next.Lat || prev.Lng != next.Lng)
                {
                    if (msl_set.TryGetValue(vm.Id, out var engagement) && engagement.PipVM != null)
                    {
                        var pip = engagement.PipVM;

                        // PIP 현재 좌표
                        PointLatLng pipPoint = new PointLatLng(pip.Latitude, pip.Longitude);

                        if (msl_set[vm.Id].Routes.Count >= 2)
                        {
                            var pipRoute = msl_set[vm.Id].Routes[0];
                            var tgtRoute = msl_set[vm.Id].Routes[1];
                            pipRoute.Points[0] = next;
                            pipRoute.Points[1] = pipPoint;

                            var targetId = vm.TargetId;

                            if (tgt_set[targetId!].Routes[vm.Id] == null) tgt_set[targetId!].Routes[vm.Id] = new List<GMapRoute>();

                            tgt_set[targetId!].Routes[vm.Id][0] = pipRoute;
                            tgt_set[targetId!].Routes[vm.Id][1] = tgtRoute;

                        }
                    }
                        

                    var line = CreatePathSegment(prev, next, Colors.LimeGreen, visible: false);
                    if (!msl_paths.TryGetValue(vm.Id, out var list))
                        msl_paths[vm.Id] = list = new List<GMapRoute>();
                    list.Add(line);
                    _map.Markers.Add(line);
                }
            }

            // 표적 위치
            foreach (var vm in tgtVMs)
            {
                PointLatLng prev = new PointLatLng(vm.Latitude, vm.Longitude);
                vm.UpdateTargetInfo();
                PointLatLng next = new PointLatLng(vm.Latitude, vm.Longitude);

                if (prev.Lat != next.Lat || prev.Lng != next.Lng)
                {
                    var line = CreatePathSegment(prev, next, Colors.Red, visible: false);
                    if (!tgt_paths.TryGetValue(vm.Id, out var list))
                        tgt_paths[vm.Id] = list = new List<GMapRoute>();
                    list.Add(line);
                    _map.Markers.Add(line);
                }
            }

  
        }

        // ======================
        // Helpers
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
                Shape = new Path
                {
                    Stroke = Brushes.LimeGreen,
                    StrokeThickness = 2,
                    Fill = Brushes.Transparent
                }
            };
        }

        private GMapRoute CreateDashedRoute(PointLatLng from, PointLatLng to, Color color)
        {
            return new GMapRoute(new List<PointLatLng> { from, to })
            {
                Shape = new Path
                {
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 3, 3 },
                    Opacity = 0.8
                }
            };
        }

        private GMapRoute CreatePathSegment(PointLatLng from, PointLatLng to, Color color, bool visible)
        {
            var route = new GMapRoute(new List<PointLatLng> { from, to })
            {
                Shape = new Path
                {
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 1.5,
                    Opacity = visible ? 0.9 : 0.3,
                    Visibility = visible ? Visibility.Visible : Visibility.Hidden
                }
            };
            return route;
        }

        private static double Deg2Rad(double d) => d * System.Math.PI / 180.0;
        private static double Rad2Deg(double r) => r * 180.0 / System.Math.PI;

        // 클릭 이벤트
        public void OnMissileClicked(string missileId) => _mapService.OnMissileClicked(missileId);
        public void OnTargetClicked(char targetId) => _mapService.OnTargetClicked(targetId);

        ~MapViewModel() => UpdateDispatcher.Instance.Unregister(UpdatePosition);
    }

    public class MissileEngagement
    {
        public List<GMapRoute> Routes { get; } = new();
        public PIPMarkerViewModel? PipVM { get; set; }
        public MissileMarkerViewModel MslVM { get; set; } = null!;
        public TargetMarkerViewModel? TgtVM { get; set; }

        public MissileEngagement(MissileMarkerViewModel mslVM)
        {
            MslVM = mslVM;
        }
    }

    public class TargetEngagement
    {
        public Dictionary<string, List<GMapRoute>> Routes { get; } = new();
        public List<PIPMarkerViewModel> PipVMs { get; } = new();
        public List<MissileMarkerViewModel> MslVMs { get; } = new();
        public TargetMarkerViewModel TgtVM { get; set; }

        public TargetEngagement(TargetMarkerViewModel tgtVM)
        {
            TgtVM = tgtVM;
        }
    }
}
