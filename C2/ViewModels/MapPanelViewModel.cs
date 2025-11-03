using C2.Services;
using C2.Views.Markers;
using CommunityToolkit.Mvvm.ComponentModel;
using GMap.NET;
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
    public partial class MapPanelViewModel : ObservableObject
    {
        private readonly MissileService _missileService;
        private readonly TargetService _targetService;
        private readonly DispatcherTimer _updateTimer;

        private readonly GMapControl _map;

        public MapPanelViewModel(GMapControl mapControl)
        {
            _map = mapControl;
            _missileService = MissileService.Instance;
            _targetService = TargetService.Instance;

            InitializeMap();

            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _updateTimer.Tick += (s, e) => UpdateAllMarkers();
            _updateTimer.Start();
        }

        private void InitializeMap()
        {
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            _map.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            _map.MinZoom = 2;
            _map.MaxZoom = 18;
            _map.Zoom = 7;
            _map.Position = new PointLatLng(36.5, 127.5);
            _map.Markers.Clear();
        }

        private void UpdateAllMarkers()
        {
            _map.Markers.Clear(); // 기존 마커 전부 초기화

            UpdateMissileMarkers();
            UpdateTargetMarkers();
            UpdatePipMarkers();
        }

        private void UpdateMissileMarkers()
        {
            foreach (var ctrl in _missileService.missileControllers)
            {
                var m = ctrl.Missile;

                // ✅ 미사일용 GMapMarker 생성
                var marker = new GMapMarker(new PointLatLng(m.Latitude, m.Longitude))
                {
                    Shape = new MissileMarker
                    {
                        DataContext = new MissileMarkerViewModel
                        {
                            Id = m.Id,
                            TargetId = m.TargetId,
                            Altitude = m.Altitude,
                            Yaw = m.Yaw,
                            Latitude = m.Latitude,
                            Longitude = m.Longitude
                        }
                    },
                    Offset = new Point(-75, -30)
                };

                _map.Markers.Add(marker);
            }
        }

        private void UpdateTargetMarkers()
        {
            //foreach (var t in _targetService.Targets)
            //{
            //    // ✅ 표적용 GMapMarker 생성
            //    var marker = new GMapMarker(new PointLatLng(t.Latitude, t.Longitude))
            //    {
            //        Shape = new TargetMarker
            //        {
            //            DataContext = new TargetMarkerViewModel
            //            {
            //                TargetId = t.Id,
            //                Altitude = t.Altitude,
            //                Yaw = t.Yaw,
            //                Latitude = t.Latitude,
            //                Longitude = t.Longitude
            //            }
            //        },
            //        Offset = new Point(-60, -25)
            //    };

            //    _map.Markers.Add(marker);
            //}
        }

        private void UpdatePipMarkers()
        {
            foreach (var ctrl in _missileService.missileControllers)
            {
                if (ctrl.PIP == null) continue;

                var p = ctrl.PIP;

                // ✅ 빨간색 X 마커
                var shape = new Canvas();
                shape.Children.Add(new Line
                {
                    X1 = -5,
                    Y1 = -5,
                    X2 = 5,
                    Y2 = 5,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                });
                shape.Children.Add(new Line
                {
                    X1 = 5,
                    Y1 = -5,
                    X2 = -5,
                    Y2 = 5,
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                });

                var marker = new GMapMarker(new PointLatLng(p.Latitude, p.Longitude))
                {
                    Shape = shape,
                    Offset = new Point(-5, -5)
                };

                _map.Markers.Add(marker);
            }
        }
    }
}
