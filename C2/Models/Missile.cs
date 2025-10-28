using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2.Models
{
    public class Missile
    {
        public string Id { get; set; }
        public string? TargetId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Speed { get; set; }
        public MissileState State { get; set; }

        public Missile(
            string Id,
            double Latitude,
            double Longitude
            ) { 
            this.Id = Id;
            this.Latitude = Latitude;
            this.Longitude = Longitude;
            this.Speed = 0.0;
            this.State = MissileState.Standby;
            
        }

        //테스트용
        public Missile(
            string Id,
            double Latitude,
            double Longitude,
            double Speed,
            MissileState State,
            string? targetId
            )
        {
            this.Id = Id;
            this.Latitude = Latitude;
            this.Longitude = Longitude;
            this.Speed = Speed;
            this.State = State;
            this.TargetId = targetId;

        }
    }

    public enum MissileState
    {
        Standby, // 대기
        InitialGuidance,    // 초기유도
        MidGuidance,        // 중기유도
        TerminalGuidance,    // 종말유도
    }
}
