using C2.Config;
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
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace C2.ViewModels
{
    public partial class MapViewModel
    {
        private readonly MapService _mapService = MapService.Instance;
        private readonly GMapControl _map;

        private GMapPolygon? _circle;

        // Marker 캐시 (뷰모델에서만 관리)
        private readonly Dictionary<string, GMapMarker> _missileMarkers = new();
        private readonly Dictionary<char, GMapMarker> _targetMarkers = new();
        private readonly Dictionary<string, GMapMarker> _pipMarkers = new();
        private readonly Dictionary<string, GMapRoute> _routes = new();
        public MapViewModel(GMapControl mapControl)
        {
            _map = mapControl;

            // 지도 초기화 (뷰모델 책임)
            //GMaps.Instance.Mode = AccessMode.ServerAndCache;
            //_map.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            _map.MapProvider = NavyDarkMapProvider.Instance;
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            _map.Manager.Mode = AccessMode.ServerOnly;
            _map.CacheLocation = "";               // SQLite 캐시 무효화
            GMaps.Instance.UseMemoryCache = false;
            _map.Manager.PrimaryCache = null;
            _map.MinZoom = 2;
            _map.MaxZoom = 12;
            _map.Zoom = 8;
            _map.Position = new PointLatLng(AppConfig.Network.Reference.Latitude, AppConfig.Network.Reference.Longitude);
            _map.CanDragMap = true;
            _map.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            _map.IgnoreMarkerOnMouseWheel = true;
            _map.MouseWheelZoomEnabled = true;

            //마커 초기화
            InitializeMissileMarkers();
            InitializePipMarkers();

            // 탐지 원
            _circle = DrawDetectionCircle(_mapService.Center, _mapService.Distance);
            _map.Markers.Add(_circle);

            // 이벤트 구독 (서비스가 스냅샷 갱신을 알려줌)
            _mapService.SnapshotUpdated += RedrawAll;

            // 주기 갱신 등록
            UpdateDispatcher.Instance.Register(_mapService.Tick); // Tick -> 10ms 주기로 스냅샷 업데이트 호출

            WeakReferenceMessenger.Default.Register<MissileLaunchMessage>(this, (r,msg) => {
                string missileId = msg.Value;
                var missileMarker = _missileMarkers.GetValueOrDefault(missileId);
                if (missileMarker!.Shape is MissileMarker2 mslMarker)
                {
                    mslMarker.SetLaunching(true);
                }
            });

            //Todo: 미사일 어보티드에서 리무브 처리

            WeakReferenceMessenger.Default.Register<TargetCreatedMessage>(this, (r, msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CreateTargetMarker(msg.Value);
                });
            });

            WeakReferenceMessenger.Default.Register<TargetRemovedMessage>(this, (r, msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    RemoveTargetMarker(msg.Value);
                });
            });
            WeakReferenceMessenger.Default.Register<MissileAbortMessage>(this, (r, msg) =>
            { // 미사일 어보트시키기

                Application.Current.Dispatcher.Invoke(() =>
                {
                    string id = msg.Value;
                    Debug.WriteLine("1");

                    _map.Markers.Remove(_missileMarkers[id]);
                    Debug.WriteLine("2");
                    
                    Debug.WriteLine($"count {_missileMarkers.Count()}");
                    _missileMarkers.Remove(id);
                    Debug.WriteLine($"count {_missileMarkers.Count()}");

                    Debug.WriteLine("3");

                    Debug.WriteLine($"count {_pipMarkers.Count()}");
                    var pipMarker = _pipMarkers[id];
                    _map.Markers.Remove(pipMarker);
                    _pipMarkers.Remove(id);
;                    Debug.WriteLine($"count {_pipMarkers.Count()}");

                    RemoveRoute(id);
                    //Debug.WriteLine("5");
                    RedrawAll();
                });
            });
            WeakReferenceMessenger.Default.Register<LaunchEndMessage>(this, (r, msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var missileMarker = _missileMarkers.GetValueOrDefault(msg.Value);
                    if (missileMarker!.Shape is MissileMarker2 mslMarker)
                    {
                        mslMarker.SetLaunching(false);
                    }
                });

                //if()
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
        private void InitializeMissileMarkers()
        {
            foreach (var missile in _mapService.GetMissiles())
            {
                var view = new MissileMarker2(missile.Id);
                view.Visibility = Visibility.Collapsed;

                var marker = new GMapMarker(new PointLatLng(missile.Latitude, missile.Longitude))
                {
                    Shape = view,
                    Offset = new Point(-20, -20),
                };

                _missileMarkers[missile.Id] = marker;
                _map.Markers.Add(marker);
            }
        }

        private void InitializePipMarkers()
        {
            foreach (var missile in _mapService.GetMissiles())
            {
                string missileId = missile.Id;

                var view = new PIPMarker2();
                view.SetVisible(false); // 초기에는 숨김 (포커스된 미사일 없음)

                var marker = new GMapMarker(new PointLatLng(missile.Latitude, missile.Longitude))
                {
                    Shape = view,
                    Offset = new Point(-10, -10)
                };

                _pipMarkers[missileId] = marker;
                _map.Markers.Add(marker);
            }
        }
        // ======================
        // Draw (스펙을 그리기만)
        // ======================

        private GMapRoute CreateRoute(Missile missile, Target target)
        {
            List<PointLatLng> points = new List<PointLatLng>();

            points.Add(ToPointLatLng(missile.Latitude, missile.Longitude));
            points.Add(ToPointLatLng(missile.PIP!.Latitude, missile.PIP.Longitude));
            points.Add(ToPointLatLng(target.CurLoc.Lat, target.CurLoc.Lon));


            var route = new GMapRoute(points)
            {
                Shape = new Path
                {
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 2,
                    StrokeDashArray = new System.Windows.Media.DoubleCollection { 3, 3 },
                    Opacity = 0.8
                }
            };

            _routes[missile.Id] = route;
            return route;
        }

        private void UpdateRoute(string missileId)
        {
            // 1) 기존 라인이 있으면 먼저 제거
            RemoveRoute(missileId);

            var missile = _mapService.GetMissiles().First(m => m.Id == missileId);
            
            var pip = missile.PIP;
            if (pip == null)
                return;

            if (missile.State == MissileState.Abort) return;
            if (missile.TargetId == null)
                return;

            var target = _mapService.GetTargets().FirstOrDefault(m => m.Id == missile.TargetId[0]);
            if (target == null)
                return;

            // 2) 새 라인을 다시 생성
            var newRoute = CreateRoute(missile, target);
            var pipMK = _pipMarkers.GetValueOrDefault(missileId);
            if (pipMK == null) return;

            newRoute.Shape.Visibility = pipMK.Shape.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            // 3) 새 라인을 지도에 추가
            _map.Markers.Add(newRoute);

            // 4) 캐시에 다시 넣기
            _routes[missileId] = newRoute;
        }
        private void RemoveRoute(string missileId)
        {
            if (_routes.TryGetValue(missileId, out var route))
            {
                _routes.Remove(missileId);
                _map.Markers.Remove(route);
                //Debug.Write("Deleted");
            }
        }

        private PointLatLng ToPointLatLng(double lat, double lng)
        {
            return new PointLatLng(lat, lng);
        }
        private void RedrawAll()
        {
            var specs = _mapService.GetMarkerSpecs().ToList();
            var aliveIds = specs.Select(s => s.Id).ToHashSet();
            CleanUpMarkers(aliveIds);
            // 1) 마커
            foreach (var mk in _mapService.GetMarkerSpecs())
            {

                if (mk.Kind == "Missile")
                {
                    var missile = _mapService.GetMissiles().First(x => x.Id == mk.Id);
                    if (missile.State == MissileState.Abort) continue;
                    //if(missile.State == MissileState.InitialGuidance) continue; 
                    // 초기 유도일때는 미사일 상태 바꾸지않고, 마커만 따로 관리
                    // 초기 PIP를 따라가는걸로 처리하고싶음

                    var missileMarker = _missileMarkers.GetValueOrDefault(mk.Id);
                    if (missileMarker == null) continue;
                    if (missileMarker.Shape is MissileMarker2 mslShape)
                    {
                        mslShape.SetYaw((double)missile.YawRaw / 100.0);
                        mslShape.SetColor(mk.Focused);
                        if (missile.State == MissileState.LaunchReady || missile.State == MissileState.Abort)
                        {
                            mslShape.SetVisible(false);
                        } else
                        {
                            mslShape.SetVisible(true);
                        }

                    }
                    if (missile.State != MissileState.InitialGuidance)
                    {
                        missileMarker.Position = new PointLatLng(mk.Lat, mk.Lon);
                        
                    } else
                    {
                        //Todo: 초기유도에는 지도상에서만 PIP 따라가도록
                        double lat = missileMarker.Position.Lat;
                        double lon = missileMarker.Position.Lng;
                        double yawDeg = missile.YawRaw / 100.0; // 0~360 도
                        double yawRad = yawDeg * Math.PI / 180.0;

                        double dt = 0.1; // 100ms (10Hz)
                        double distance = missile.Speed * dt; // m 단위

                        // 4. 거리 → 위경도 변환
                        var newPos = MoveLatLon(lat, lon, distance, yawDeg);
                        missileMarker.Position = new PointLatLng(newPos.Lat, newPos.Lng);
                    }
                        
                }
                else if (mk.Kind == "Target")
                {
                    char tid = mk.Id[0];
                    var target = _mapService.GetTargets().First(x => x.Id == tid);
                    var targetMarker = _targetMarkers.GetValueOrDefault(tid);
                    if (targetMarker == null) continue;
                    if (targetMarker!.Shape is TargetMarker2 tgtShape)
                    {
                        tgtShape.SetYaw((double)target.Yaw/100.0);
                        tgtShape.SetFocused(mk.Focused);
                    }
                    targetMarker.Position = new PointLatLng(mk.Lat, mk.Lon);
                }
                else // "PIP"
                {
                    var missileId = mk.Id.Replace("PIP::", "");
                    var missile = _mapService.GetMissiles().First(x => x.Id == missileId);

                    var pip = missile.PIP;
                    if (pip == null) continue;
                    var pipMarker = _pipMarkers.GetValueOrDefault(missileId);
                    
                    if (pipMarker == null) continue;

                    if (pipMarker.Shape is PIPMarker2 pipShape)
                    {
                        if (missile.State == MissileState.Abort)
                        {
                            pipShape.SetVisible(false);
                            continue;
                        }
                        pipShape.SetVisible(mk.Focused);

                        if (pipShape.AfterLaunch == false
                            && (missile.State == MissileState.MidGuidance|| missile.State == MissileState.TerminalGuidance))
                        {
                            pipShape.LaunchUpdatePIP();
                        }
                    }

                    

                    pipMarker.Position = new PointLatLng(mk.Lat, mk.Lon);
                }
            }

            // 2) 라인
            foreach (var msl in _mapService.GetMissiles())
            {
                UpdateRoute(msl.Id);
            }
            /*
           // 3) 경로
           foreach (var rt in _mapService.GetRouteSpecs())
           {
               _map.Markers.Add(CreatePath(rt.Points, rt.Color));
           }*/
        }

        private void CleanUpMarkers(HashSet<string> aliveIds)
        {
            var removeMsl = _missileMarkers.Keys
                .Where(id => !aliveIds.Contains(id))
                .ToList();

            foreach (var id in removeMsl)
            {
                _map.Markers.Remove(_missileMarkers[id]);
                _missileMarkers.Remove(id);
            }

            // ----------------------
            var removePip = _pipMarkers.Keys
                .Where(id => !aliveIds.Contains(id))
                .ToList();

            foreach (var id in removePip)
            {
                _map.Markers.Remove(_pipMarkers[id]);
                _pipMarkers.Remove(id);
            }

            var removeTgt = _targetMarkers.Keys
                .Where(id => !aliveIds.Contains(id.ToString()))
                .ToList();

            foreach (var id in removeTgt)
            {
                _map.Markers.Remove(_targetMarkers[id]);
                _targetMarkers.Remove(id);
            }
        }

        // ======================
        // 표적관리
        // ======================
        private void CreateTargetMarker(char id)
        {
            if (_targetMarkers.ContainsKey(id))
                return;

            var target = _mapService.GetTargets().FirstOrDefault(m=>m.Id == id);
            if (target == null)
                return;

            var shape = new TargetMarker2(id);   // 너가 만든 픽토그램 Shape
            var marker = new GMapMarker(new PointLatLng(target.CurLoc.Lat, target.CurLoc.Lon))
            {
                Shape = shape,
                Offset = new System.Windows.Point(-20, -20) // 중심 정렬
            };

            _targetMarkers[id] = marker;
            _map.Markers.Add(marker);
        }

        private void RemoveTargetMarker(char id)
        {
            if (_targetMarkers.TryGetValue(id, out var marker))
            {
                _map.Markers.Remove(marker);
                _targetMarkers.Remove(id);
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
                    Stroke = new SolidColorBrush(Color.FromRgb(232, 247, 255)),
                    StrokeThickness = 2.5,
                    StrokeDashArray = new DoubleCollection { 4, 6 }, // 레이더 점선 느낌
                    Opacity = 0.8,
                    Fill = Brushes.Transparent,
                    Effect = new DropShadowEffect
                    {
                        Color = Color.FromRgb(0, 255, 200),
                        BlurRadius = 25,
                        ShadowDepth = 0,
                        Opacity = 0.7
                    }
                }
            };
        }

        private static double Deg2Rad(double d) => d * System.Math.PI / 180.0;
        private static double Rad2Deg(double r) => r * 180.0 / System.Math.PI;
        private PointLatLng MoveLatLon(double lat, double lon, double distance, double bearingDeg)
        {
            const double R = 6371000.0; // 지구 반경 (m)

            double latRad = lat * Math.PI / 180.0;
            double lonRad = lon * Math.PI / 180.0;
            double bearingRad = bearingDeg * Math.PI / 180.0;

            double newLatRad = Math.Asin(
                Math.Sin(latRad) * Math.Cos(distance / R) +
                Math.Cos(latRad) * Math.Sin(distance / R) * Math.Cos(bearingRad)
            );

            double newLonRad = lonRad +
                Math.Atan2(
                    Math.Sin(bearingRad) * Math.Sin(distance / R) * Math.Cos(latRad),
                    Math.Cos(distance / R) - Math.Sin(latRad) * Math.Sin(newLatRad)
                );

            return new PointLatLng(
                newLatRad * 180.0 / Math.PI,
                newLonRad * 180.0 / Math.PI
            );
        }
    }
}
