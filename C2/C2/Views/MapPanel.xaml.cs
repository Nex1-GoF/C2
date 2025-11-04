using C2.Models;
using C2.Services;
using C2.ViewModels;
using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace C2.Views
{
    /// <summary>
    /// MapPanel.xaml에 대한 상호 작용 논리
    /// </summary>
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

        // 지도 클릭 시 로그 띄우기 (테스트코드)
        private void OnMapClick(object sender, MouseButtonEventArgs e)
        {
            // 클릭 위치 (픽셀 기준)
            var point = e.GetPosition(PART_Map);
            
            
            // 테스트코드 => 지울 예정 => 바로 모델 호출
            LogService logService = LogService.Instance;
            

            //------ 포커스 모드 ---------//
            GMapMarker? clickedMarker = null;
            double minDist = double.MaxValue;

            foreach (var marker in PART_Map.Markers)
            {
                if (marker.Shape is not FrameworkElement shape) continue;

                var pos = PART_Map.FromLatLngToLocal(marker.Position);
                double dx = point.X - pos.X;
                double dy = point.Y - pos.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist < 40 && dist < minDist)
                {
                    clickedMarker = marker;
                    minDist = dist;
                }
            }

            if (clickedMarker == null)
            {
                string mapClickText = $"지도 클릭: {point.X},{point.Y}";
                logService.AddLog(MessageType.System, mapClickText);
                return;
            }

            // 2️ 클릭된 객체의 ViewModel 추출

            if (clickedMarker.Shape is FrameworkElement frameworkElement && frameworkElement.DataContext is MissileMarkerViewModel missileVm)
            {
                _vm.FocusMissileMarker(missileVm);
                string mapClickText = "미사일 포커스 변경";
                logService.AddLog(MessageType.System, mapClickText);

            }
            else if (clickedMarker.Shape is FrameworkElement frameworkElementTarget && frameworkElementTarget.DataContext is TargetMarkerViewModel targetVm)
            {
                string mapClickText = "타겟 포커스 변경";
                logService.AddLog(MessageType.System, mapClickText);
                _vm.FocusTargetMarker(targetVm);
            }


        }


    }
}
