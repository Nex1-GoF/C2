using GMap.NET;
using GMap.NET.WindowsPresentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Shapes;

namespace C2.Services
{
    public class MapService
    {
        private static MapService _instance;
        public static MapService Instance => _instance ??= new MapService();

        private GMapControl? _map;
        public GMapControl? Map => _map;

        public PointLatLng Center { get; set; } = new PointLatLng(37.5665, 126.9780);
        public double Distance { get; set; } = 250_000;

        // 🔹 실제 지도에 표시될 마커/경로 컬렉션

        private MapService() { }

    }
}
