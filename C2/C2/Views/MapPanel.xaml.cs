using C2.Models;
using C2.Services;
using C2.ViewModels;
using GMap.NET.WindowsPresentation;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace C2.Views
{
    public partial class MapPanel : UserControl
    {
        private readonly MapViewModel _vm;

        public MapPanel()
        {
            InitializeComponent();
            _vm = new MapViewModel(PART_Map);
            DataContext = _vm;
            PART_Map.MouseLeftButtonUp += OnMapClick;
        }

        // 지도 클릭 시: 가장 가까운 마커를 찾아서 포커스 요청을 ViewModel(→MapService)로 전달
        private void OnMapClick(object sender, MouseButtonEventArgs e)
        {
            var logService = LogService.Instance;

            // 1) 클릭 위치 (픽셀 좌표)
            var clickPt = e.GetPosition(PART_Map);

            // 2) 가장 가까운 마커 탐색
            GMapMarker? clickedMarker = null;
            double minDist = double.MaxValue;

            foreach (var marker in PART_Map.Markers)
            {
                if (marker.Shape is not FrameworkElement) continue;

                var markerPx = PART_Map.FromLatLngToLocal(marker.Position);
                double dx = clickPt.X - markerPx.X;
                double dy = clickPt.Y - markerPx.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < 40 && dist < minDist) // 픽셀 기준 허용 반경
                {
                    clickedMarker = marker;
                    minDist = dist;
                }
            }

            if (clickedMarker == null)
            {
                logService.AddLog(MessageType.System, $"지도 클릭: {clickPt.X:0},{clickPt.Y:0}");
                return;
            }

            // 3) 마커 타입에 따라 포커스 위임 (뷰모델 → MapService)
            if (clickedMarker.Shape is FrameworkElement fe1 && fe1.DataContext is MissileMarkerViewModel mslVm)
            {
                _vm.OnMissileClicked(mslVm.Id); // ✅ 서비스로 위임
                logService.AddLog(MessageType.System, "미사일 포커스 변경");
            }
            else if (clickedMarker.Shape is FrameworkElement fe2 && fe2.DataContext is TargetMarkerViewModel tgtVm)
            {
                _vm.OnTargetClicked(tgtVm.DefaultID); // ✅ 서비스로 위임
                logService.AddLog(MessageType.System, "타겟 포커스 변경");
            }
            // 필요하면 PIP도 추가 가능:
            // else if (clickedMarker.Shape is FrameworkElement fe3 && fe3.DataContext is PIPMarkerViewModel pipVm) { ... }
        }
    }
}
