using C2.Models;
using C2.Services;
using C2.ViewModels;
using C2.Views.Markers;
using GMap.NET.WindowsPresentation;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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

            var clickPt = e.GetPosition(PART_Map);

            GMapMarker? clickedMarker = null;
            double minDist = double.MaxValue;

            foreach (var marker in PART_Map.Markers)
            {
                // Shape 없으면 스킵
                if (marker.Shape is not FrameworkElement shape)
                    continue;

                // 1) PIP는 클릭 대상에서 제외 (원하면 포함)
                if (shape is PIPMarker2)
                    continue;

                if (shape.Visibility != Visibility.Visible)
                    continue;

                // 2) 픽셀 거리 계산
                var markerPx = PART_Map.FromLatLngToLocal(marker.Position);
                double dx = clickPt.X - markerPx.X;
                double dy = clickPt.Y - markerPx.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < 40 && dist < minDist)
                {
                    clickedMarker = marker;
                    minDist = dist;
                }
            }

            if (clickedMarker == null)
            {
                logService.AddLog(MessageType.System,
                    $"지도 클릭: {clickPt.X:0},{clickPt.Y:0}");
                return;
            }

            // 3) 타입별로 처리
            if (clickedMarker.Shape is MissileMarker2 missileShape)
            {
                string id = missileShape.MissileId; // ← 여길 구현해야 함
                _vm.OnMissileClicked(id);
                logService.AddLog(MessageType.System, $"미사일 {id} 클릭");
            }
            else if (clickedMarker.Shape is TargetMarker2 targetShape)
            {
                char id = targetShape.TargetId;     // ← 여길 구현해야 함
                _vm.OnTargetClicked(id);
                logService.AddLog(MessageType.System, $"타겟 {id} 클릭");
            }
        }
    }
}
