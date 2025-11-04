using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace C2.Models
{
    public class Target 
    {
        public char Id { get; set; }
        public char DetectedType { get; set; }
        public int Speed { get; set; }     // m/s
        public int Altitude { get; set; }  // mprivate int _yaw;
        public int Yaw{ get; private set; }

        public void setYaw() => Yaw = 0;

        public (double Lat, double Lon) EndLoc { get; set; }
        public DateTime? DetectTime { get; set; }
        public (double Lat, double Lon) CurLoc { get; private set; }
        public string CurYawDisplay => $"{Yaw:F5}";
        public string CurLocDisplay => $"{CurLoc.Lat:F5}, {CurLoc.Lon:F5}";
        public string EndLocDisplay => $"{EndLoc.Lat:F5}, {EndLoc.Lon:F5}";
        public List<(double Lat, double Lon)> PathHistory { get; } = new();
    }
}