using C2.ViewModels;
using GMap.NET;
using GMap.NET.MapProviders;
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
    public partial class MapPanel : UserControl
    {

        public MapPanel()
        {
            InitializeComponent();

            //PART_Map = new GMapControl(); // GMapControl 객체 초기화

            Loaded += (s, e) =>
            {
                OnLoaded(s, e);
            };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            GMaps.Instance.Mode = AccessMode.ServerOnly; //(ServerAndCache->코드 실행->CacheOnly로 바꾸기)
            //PART_Map.CacheLocation = @"C:\MapCache"; // C:\MapCache에 지도데이터를 넣어야겠지?
            PART_Map.MapProvider = OpenStreetMapProvider.Instance;
            PART_Map.MinZoom = 2;
            PART_Map.MaxZoom = 18;
            PART_Map.Zoom = 6;
            PART_Map.Position = new PointLatLng(37.5665, 126.9780);
        }
    }
}
