using C2.Messages;
using C2.Models;
using C2.Services;
using C2.Views.Markers;
using CommunityToolkit.Mvvm.Messaging;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;

namespace C2.ViewModels
{
    public partial class MapViewModel
    {
        private readonly MapService _mapService = MapService.Instance;
        private readonly MissileService _missileService = MissileService.Instance;

        private readonly GMapControl _map;

        private GMapPolygon _circle;  // 탐지 원 (고정)

        // ================================
        // 관리 컬렉션
        // ================================
        private readonly List<MissileMarkerViewModel> mslVMs = new();
        private readonly List<TargetMarkerViewModel> tgtVMs = new();
        private readonly List<PIPMarkerViewModel> pipVMs = new();

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

            // 미사일 초기화
            foreach (Missile missile in _missileService.GetAllMissiles())
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var vm = new MissileMarkerViewModel(missile);
                    mslVMs.Add(vm);

                    // 지도에 마커 추가
                    var marker = new GMapMarker(new PointLatLng(vm.Latitude, vm.Longitude))
                    {
                        Shape = new MissileMarker { DataContext = vm },
                        Offset = new System.Windows.Point(-25, -25)
                    };
                    vm.BindMarker(marker);
                    _map.Markers.Add(marker);
                    // msl_set 등록
                    if (!msl_set.ContainsKey(vm.Id))
                        msl_set[vm.Id] = new MissileEngagement(vm);
                });
            }

            // 주기 갱신
            UpdateDispatcher.Instance.Register(UpdatePosition);

            // ================================
            // 메시지 수신 등록
            // ================================
            WeakReferenceMessenger.Default.Register<TargetCreatedMessage>(this, (r, m) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Target ViewModel 생성 및 마커 등록
                    var vm = new TargetMarkerViewModel(m.Target);
                    tgtVMs.Add(vm);

                    var marker = new GMapMarker(new PointLatLng(vm.Latitude, vm.Longitude))
                    {
                        Shape = new TargetMarker { DataContext = vm },
                        Offset = new System.Windows.Point(-25, -25)
                    };
                    vm.BindMarker(marker);
                    _map.Markers.Add(marker);

                    // 2️⃣ tgt_set 엔트리 초기화
                    string targetId = vm.DefaultID.ToString();
                    if (!tgt_set.ContainsKey(targetId))
                    {
                        var engagement = new TargetEngagement(vm);
                        tgt_set[targetId] = engagement;

                        // ✅ Target 자체 궤적 Path 생성 (빨간 실선)
                        var targetPath = new GMapRoute(new List<PointLatLng> { new PointLatLng(vm.Latitude, vm.Longitude) })
                        {
                            Shape = new Path
                            {
                                Stroke = Brushes.Red,
                                StrokeThickness = 1.8,
                                Opacity = 0.8,
                                Visibility = Visibility.Visible
                            }
                        };

                        // 🔗 지도에 Path 추가 및 관리 구조 등록
                        _map.Markers.Add(targetPath);
                    }

                });
            });

            // 1️⃣ 미사일 선택 메시지
            WeakReferenceMessenger.Default.Register<MissileSelectedMessage>(this, (r, m) =>
            {
               
                foreach (var msl_set_value in msl_set.Values)
                {
                    // 미사일 업데이트포커스
                    msl_set_value.MslVM.UpdateFocus(false);

                    // 타겟 업데이트포커스
                    if(msl_set_value.TgtVM != null)
                        msl_set_value.TgtVM.UpdateFocus(false);

                    // 라인 보여주기
                    if (msl_set_value.Line != null) 
                        msl_set_value.Line.Shape.Visibility = Visibility.Hidden;

                    // 경로 보여주기
                    if (msl_set_value.Path != null)
                        msl_set_value.Path.Shape.Visibility = Visibility.Hidden;

                    // PIP
                    if(msl_set_value.PipVM != null)
                        msl_set_value.PipVM.UpdateVisible(false);
                }

                string? id = m.Value;
                if (id == null) return;

                var selected = id!;
                var currentMissileSet = msl_set[selected];

                currentMissileSet.MslVM.UpdateFocus(true);

                // 표적 보여주기
                if (currentMissileSet.TgtVM != null)
                    currentMissileSet.TgtVM.UpdateFocus(true);

                // 라인 보여주기
                if (currentMissileSet.Line != null)
                    currentMissileSet.Line.Shape.Visibility = Visibility.Visible;

                // 경로 보여주기
                if (currentMissileSet.Path != null)
                    currentMissileSet.Path.Shape.Visibility = Visibility.Visible;

                // PIP
                if (currentMissileSet.PipVM != null)
                    currentMissileSet.PipVM.UpdateVisible(true);


            });

            WeakReferenceMessenger.Default.Register<TargetSelectedMessage>(this, (r, m) =>
            {

                // 전체 포커스 해제
                foreach (var tgt_set_value in tgt_set.Values)
                {
                    // 경로
                    if(tgt_set_value.Paths!= null)
                        tgt_set_value.Paths.Shape.Visibility = Visibility.Hidden;
                    // 라인
                    foreach (var line in tgt_set_value.Line.Values)
                        line.Shape.Visibility = Visibility.Hidden;
                    // pip
                    foreach (var pip in tgt_set_value.PipVMs)
                        pip.UpdateVisible(false);
                    // 미사일
                    foreach (var msl in tgt_set_value.MslVMs)
                        msl.UpdateFocus(false);

                    // 표적
                    tgt_set_value.TgtVM.UpdateFocus(false);
                }
                

                char? selectedTargetId = m.Value;

                if (selectedTargetId == null) {
                    // 새로 지정할게 없으면 포커스 해제만
                    return;
                }

                var selected = selectedTargetId.ToString();

                // 타겟 포커스
                var currentTargetSet = tgt_set[selected!];
                currentTargetSet.TgtVM.UpdateFocus(true);

                // 표적 경로 보여주기
                if(currentTargetSet.Paths != null)
                    currentTargetSet.Paths.Shape.Visibility = Visibility.Visible;

                // 관련 미사일 포커스, 연결 라인 보여주기
                foreach(var mslVM in currentTargetSet.MslVMs){
                    mslVM.UpdateFocus(true);
                    if(currentTargetSet.Line.ContainsKey(mslVM.Id))
                        currentTargetSet.Line[mslVM.Id].Shape.Visibility = Visibility.Visible;
                }

                // PIP 보여주기
                foreach (var pipVM in currentTargetSet.PipVMs) { 
                    pipVM.UpdateVisible(true);
                }
            });





            // 2️ 교전 할당 메시지
            WeakReferenceMessenger.Default.Register<EngagementAssignedMessage>(this, (r, m) =>
            {
                string missileId = m.Value.MissileId;
                string targetId = m.Value.TargetId;

                var mslVM = mslVMs.FirstOrDefault(v => v.Id == missileId);
                var tgtVM = tgtVMs.FirstOrDefault(v => v.DefaultID.ToString() == targetId);

                if (mslVM == null || tgtVM == null)
                    return;


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

                // PIP 생성
                var pipVM = new PIPMarkerViewModel(new PIP(pipLat, pipLon, pipAlt), missileId);
                var pipMarker = new GMapMarker(new PointLatLng(pipLat, pipLon))
                {
                    Shape = new PIPMarker { DataContext = pipVM }
                };
                pipVM.BindMarker(pipMarker);
                pipVMs.Add(pipVM);
                _map.Markers.Add(pipMarker);

                // 초기 라인 설정
                PointLatLng fromMsl = new PointLatLng(mslVM.Latitude, mslVM.Longitude);
                PointLatLng pip = new PointLatLng(pipLat, pipLon);
                PointLatLng toTgt = new PointLatLng(tgtVM.Latitude, tgtVM.Longitude);

                var dashedLine = CreateDashedRoute(new List<PointLatLng> { fromMsl, pip, toTgt }, Colors.Black, false);
                _map.Markers.Add(dashedLine);


                var mEntry = msl_set[missileId];
                mEntry.PipVM = pipVM;
                mEntry.TgtVM = tgtVM;
                mEntry.Line = dashedLine;       // 하나의 라인으로 통합


                var tEntry = tgt_set[targetId];
                if (!tEntry.PipVMs.Contains(pipVM))
                    tEntry.PipVMs.Add(pipVM);
                if (!tEntry.MslVMs.Contains(mslVM))
                    tEntry.MslVMs.Add(mslVM);

                // TargetEngagement에도 동일 라인 저장
                tEntry.Line[missileId] = dashedLine;
            });
        }

        // ======================
        // Update
        // ======================
        public void UpdatePosition()
        {
            // 1️⃣ PIP 위치 갱신
            foreach (var pipVM in pipVMs)
                pipVM.UpdatePIP();

            // 2️⃣ 표적 위치 + 경로 갱신
            foreach (var tgtVM in tgtVMs)
            {
                var targetId = tgtVM.DefaultID.ToString();
                PointLatLng before = new PointLatLng(tgtVM.Latitude, tgtVM.Longitude);  
                tgtVM.UpdateTargetInfo();
                PointLatLng after = new PointLatLng(tgtVM.Latitude, tgtVM.Longitude);
                var targetEngagement = tgt_set.GetValueOrDefault(targetId);
                targetEngagement?.Paths?.Points.Add(new PointLatLng(tgtVM.Latitude, tgtVM.Longitude));

                var targetPath = new GMapRoute(new List<PointLatLng> { before, after})
                {
                    Shape = new Path
                    {
                        Stroke = Brushes.Red,
                        StrokeThickness = 1.8,
                        Opacity = 0.8,
                        Visibility = Visibility.Visible
                    }
                };
                _map.Markers.Add(targetPath!);
            }

            // 3️⃣ 미사일 위치 + 경로 + 라인 갱신
            foreach (var mslVM in mslVMs)
            {
                string missileId = mslVM.Id;
                mslVM.UpdateMissileInfo();

                var engagement = msl_set.GetValueOrDefault(missileId);
                if (engagement == null) continue;

                // ✅ Path 누적
                engagement.Path?.Points.Add(new PointLatLng(mslVM.Latitude, mslVM.Longitude));

                // ✅ Line 재생성 로직
                if (engagement.Line != null)
                {
                    // 기존 라인 제거
                    if (_map.Markers.Contains(engagement.Line))
                        _map.Markers.Remove(engagement.Line);
                }

                // 새 좌표 계산
                var mslPoint = new PointLatLng(mslVM.Latitude, mslVM.Longitude);
                var tgtVM = engagement.TgtVM;
                var pipVM = engagement.PipVM;

                if (tgtVM == null || pipVM == null)
                    continue;

                var pipPoint = new PointLatLng(pipVM.Latitude, pipVM.Longitude);
                var tgtPoint = new PointLatLng(tgtVM.Latitude, tgtVM.Longitude);

                // 새 라인 생성
                var newLine = CreateDashedRoute(
                    new List<PointLatLng> { mslPoint, pipPoint, tgtPoint },
                    Colors.Black,
                    pipVM.IsVisible
                );


                // 맵 및 구조체에 반영
                _map.Markers.Add(newLine);
                engagement.Line = newLine;
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

        private GMapRoute CreateDashedRoute(List<PointLatLng> points, Color color, bool isVisible)
        {
            return new GMapRoute(points)
            {
                Shape = new Path
                {
                    Stroke = new SolidColorBrush(color),
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 3, 3 }, // 점선 패턴
                    Opacity = 0.9,
                    Visibility = isVisible ? Visibility.Visible : Visibility.Hidden
                }
            };
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
        public GMapRoute? Path { get; set; }
        public GMapRoute? Line { get; set; }             // 미사일 → PIP
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
        public GMapRoute? Paths { get; set; }
        public Dictionary<string, GMapRoute> Line { get; } = new();
        public List<PIPMarkerViewModel> PipVMs { get; } = new();
        public List<MissileMarkerViewModel> MslVMs { get; } = new();
        public TargetMarkerViewModel TgtVM { get; set; }

        public TargetEngagement(TargetMarkerViewModel tgtVM)
        {
            TgtVM = tgtVM;
        }
    }
}
