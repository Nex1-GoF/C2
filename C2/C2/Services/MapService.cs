using C2.Models;
using GMap.NET;
using GMap.NET.WindowsPresentation;
using System;
using System.Linq;

namespace C2.Services
{
    public class MapService
    {
        private static MapService _instance;
        public static MapService Instance => _instance ??= new MapService();

        // ===========================================
        // 🗺️ 지도 기본 속성
        // ===========================================
        private GMapControl? _map;
        public GMapControl? Map => _map;

        public PointLatLng Center { get; set; } = new PointLatLng(37.5665, 126.9780); // 서울 시청 기준
        public double Distance { get; set; } = 250_000; // 250km 탐지 반경

        private MapService()
        {
            // 10ms 주기로 포커스 유효성 검사
        }

        public void Initialize(GMapControl mapControl)
        {
            _map = mapControl;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;
            _map.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            _map.MinZoom = 2;
            _map.MaxZoom = 18;
            _map.Zoom = 7;
            _map.Position = new PointLatLng(36.5, 127.5);
            _map.CanDragMap = true;
            _map.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            _map.IgnoreMarkerOnMouseWheel = true; // 마커 위에서도 휠 줌 동작
            _map.MouseWheelZoomEnabled = true;    // 마우스 휠로 줌 가능

            
        }

    }
}
