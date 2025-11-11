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

        private readonly Dictionary<string, List<GMapPolygon>> msl_paths = new();
        private readonly Dictionary<string, List<GMapPolygon>> tgt_paths = new();

        private readonly Dictionary<string, (List<GMapPolygon> routes,
                                             PIPMarkerViewModel? pipVM,
                                             MissileMarkerViewModel mslVM,
                                             TargetMarkerViewModel tgtVM)> msl_set = new();

        private readonly Dictionary<string, (List<GMapPolygon> routes,
                                             List<PIPMarkerViewModel> pipVMs,
                                             List<MissileMarkerViewModel> mslVMs,
                                             TargetMarkerViewModel tgtVM)> tgt_set = new();



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

            // 주기 갱신 등록
            UpdateDispatcher.Instance.Register(UpdatePosition);

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
            WeakReferenceMessenger.Default.Register<EngagementAssignedMessage>(this, (r, m) =>
            {
                string missileId = m.Value.MissileId;
                string targetId = m.Value.TargetId;

                // ===== 1️ mslVM / tgtVM 탐색 =====
                var mslVM = mslVMs.FirstOrDefault(v => v.Id == missileId);
                var tgtVM = tgtVMs.FirstOrDefault(v => v.DefaultID.ToString() == targetId);
                var pipVM = pipVMs.FirstOrDefault(v => v.MissileId == missileId);

                if (mslVM == null || tgtVM == null)
                    return;

                // ===== 2️ msl_set 갱신 =====
                if (!msl_set.ContainsKey(missileId))
                {
                    msl_set[missileId] = (new List<GMapPolygon>(), pipVM, mslVM, tgtVM);
                }
                else
                {
                    var current = msl_set[missileId];
                    current.mslVM = mslVM;
                    current.tgtVM = tgtVM;
                    current.pipVM = pipVM;
                    msl_set[missileId] = current;
                }

                // ===== 3️ tgt_set 갱신 =====
                if (!tgt_set.ContainsKey(targetId))
                {
                    tgt_set[targetId] = (new List<GMapPolygon>(),
                                         new List<PIPMarkerViewModel>(),
                                         new List<MissileMarkerViewModel>(),
                                         tgtVM);
                }

                // 중복 방지 후 추가
                if (!tgt_set[targetId].mslVMs.Contains(mslVM))
                    tgt_set[targetId].mslVMs.Add(mslVM);
                if (pipVM != null && !tgt_set[targetId].pipVMs.Contains(pipVM))
                    tgt_set[targetId].pipVMs.Add(pipVM);
            });
            WeakReferenceMessenger.Default.Register<PipCalculatedMessage>(this, (r, m) =>
            {
                string missileId = m.Value.MissileId;
                double pipLat = m.Value.Lat;
                double pipLon = m.Value.Lon;
                short pipAlt = m.Value.Alt;

                // ✅ 1️⃣ 미사일 VM 찾기
                var mslVM = mslVMs.FirstOrDefault(v => v.Id == missileId);
                if (mslVM == null) return;

                // ✅ 2️⃣ 기존 PIP 있으면 갱신, 없으면 생성
                var pipVM = pipVMs.FirstOrDefault(p => p.MissileId == missileId);
                if (pipVM == null)
                {
                    pipVM = new PIPMarkerViewModel(new PIP(pipLat, pipLon, pipAlt), missileId);
                    pipVMs.Add(pipVM);
                }
                else
                {
                    pipVM.UpdatePIP(new PIP(pipLat, pipLon, pipAlt));
                }

                // ✅ 3️⃣ PIP 마커 추가
                var pipMarker = new GMapMarker(new PointLatLng(pipLat, pipLon))
                {
                    Shape = new PIPMarker { DataContext = pipVM }
                };
                _map.Markers.Add(pipMarker);

                // ✅ 4️⃣ 미사일 ↔ PIP 점선(Route) 추가
                PointLatLng from = new PointLatLng(mslVM.Latitude, mslVM.Longitude);
                PointLatLng to = new PointLatLng(pipLat, pipLon);
                var route = CreateDashedRoute(from, to, Colors.SkyBlue);
                _map.Markers.Add(route);

                // ✅ 5️⃣ msl_set 갱신
                if (!msl_set.ContainsKey(missileId))
                {
                    // 타겟은 나중에 EngagementAssignedMessage로 연결될 수 있음
                    msl_set[missileId] = (new List<GMapPolygon>(), pipVM, mslVM, null!);
                }
                msl_set[missileId].routes.Add(route);
            });
        }

        public void UpdatePosition()
        {
            // Circle 유지
            if (!_map.Markers.Contains(_circle))
                _map.Markers.Insert(0, _circle);

            // -------------------------
            // 1️⃣ 미사일 경로 갱신
            // -------------------------
            foreach (var vm in mslVMs)
            {
                PointLatLng prev = new PointLatLng(vm.Latitude, vm.Longitude);
                vm.UpdateMissileInfo();
                PointLatLng next = new PointLatLng(vm.Latitude, vm.Longitude);

                // 위치가 변했을 때만 추가
                if (prev.Lat != next.Lat || prev.Lng != next.Lng)
                {
                    var line = CreatePathSegment(prev, next, Colors.LimeGreen, visible: false);

                    if (!msl_paths.TryGetValue(vm.Id, out var list))
                    {
                        list = new List<GMapPolygon>();
                        msl_paths[vm.Id] = list;
                    }

                    list.Add(line);
                    _map.Markers.Add(line);
                }
            }

            // -------------------------
            // 2️⃣ 표적 경로 갱신
            // -------------------------
            foreach (var vm in tgtVMs)
            {
                PointLatLng prev = new PointLatLng(vm.Latitude, vm.Longitude);
                vm.UpdateTargetInfo();
                PointLatLng next = new PointLatLng(vm.Latitude, vm.Longitude);

                if (prev.Lat != next.Lat || prev.Lng != next.Lng)
                {
                    var line = CreatePathSegment(prev, next, Colors.Red, visible: false);

                    if (!tgt_paths.TryGetValue(vm.Id, out var list))
                    {
                        list = new List<GMapPolygon>();
                        tgt_paths[vm.Id] = list;
                    }

                    list.Add(line);
                    _map.Markers.Add(line);
                }
            }

            // -------------------------
            // 3️⃣ PIP 위치 갱신
            // -------------------------
            foreach (var vm in pipVMs)
                vm.UpdatePIP();
        }

        private GMapPolygon CreatePathSegment(PointLatLng from, PointLatLng to, Color color, bool visible)
        {
            var pts = new List<PointLatLng> { from, to };

            var polygon = new GMapPolygon(pts)
            {
                Shape = new Path
                {
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 1.5,
                    Opacity = visible ? 0.9 : 0.3,
                    Visibility = visible ? Visibility.Visible : Visibility.Hidden
                }
            };

            return polygon;
        }


        // 뷰에서 클릭 시 호출
        public void OnMissileClicked(string missileId)
        {
            _mapService.OnMissileClicked(missileId);
        }
        public void OnTargetClicked(char targetId)
        {
            _mapService.OnTargetClicked(targetId);
        }

        ~MapViewModel()
        {
            UpdateDispatcher.Instance.Unregister(UpdatePosition);
        }

        // ======================
        // Draw (스펙을 그리기만)
        // ======================
        //private void RedrawAll()
        //{
        //    _map.Markers.Clear();
        //    if (_circle != null) _map.Markers.Add(_circle);

        //    // 1) 마커
        //    foreach (var mk in _mapService.GetMarkerSpecs())
        //    {
        //        FrameworkElement shape;

        //        if (mk.Kind == "Missile")
        //        {
        //            if (!_missileVMs.TryGetValue(mk.Id, out var vm))
        //            {
        //                var m = _mapService.GetMissiles().First(x => x.Id == mk.Id);
        //                _missileVMs[mk.Id] = vm = new MissileMarkerViewModel(m);
        //            }
        //            vm.UpdateFocus(mk.Focused);
        //            shape = new MissileMarker { DataContext = vm };
        //        }
        //        else if (mk.Kind == "Target")
        //        {
        //            char tid = mk.Id[0];
        //            if (!_targetVMs.TryGetValue(tid, out var vm))
        //            {
        //                var t = _mapService.GetTargets().First(x => x.Id == tid);
        //                _targetVMs[tid] = vm = new TargetMarkerViewModel(t);
        //            }
        //            vm.UpdateFocus(mk.Focused);
        //            shape = new TargetMarker { DataContext = vm };
        //        }
        //        else // "PIP"
        //        {
        //            var missileId = mk.Id.Replace("PIP::", "");
        //            if (!_pipVMs.TryGetValue(missileId, out var vm))
        //            {
        //                var m = _mapService.GetMissiles().First(x => x.Id == missileId);
        //                if (m.PIP == null) continue;
        //                _pipVMs[missileId] = vm = new PIPMarkerViewModel(m.PIP, missileId);
        //            }
        //            vm.UpdateVisible(mk.Visible);
        //            shape = new PIPMarker { DataContext = vm };
        //        }

        //        var marker = new GMapMarker(new PointLatLng(mk.Lat, mk.Lon))
        //        {
        //            Shape = shape,
        //            Offset = (mk.Kind == "PIP") ? new Point(0, 0) : new Point(-25, -25)
        //        };
        //        _map.Markers.Add(marker);
        //    }

        //    // 2) 라인
        //    foreach (var ln in _mapService.GetLineSpecs())
        //    {
        //        var pts = new List<PointLatLng> { ln.From, ln.To };
        //        _map.Markers.Add(CreateDashed(pts)); // "DashedBlack" 스타일만 사용
        //    }

        //    // 3) 경로
        //    foreach (var rt in _mapService.GetRouteSpecs())
        //    {
        //        _map.Markers.Add(CreatePath(rt.Points, rt.Color));
        //    }
        //}

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
