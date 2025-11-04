using System;
using System.Security.Policy;

namespace C2.Models
{
    /// <summary>
    /// 유도탄 실시간 데이터 모델 (DataLink / Telemetry 기반)
    /// </summary>
    /// 
    /// <summary>
    /// 비행 상태 enum
    /// </summary>
    public enum MissileState : byte
    {
        LaunchReady = 1,   // 발사 준비
        InitialGuidance,   // 초기 유도
        MidGuidance,       // 중기 유도
        TerminalGuidance,  // 종말 유도
        Abort              // 중단
    }

    public class Missile
    {
        /// <summary> 위도 (실제값 × 1e7) </summary>
        public int LatitudeRaw { get; set; }
        public int LongitudeRaw { get; set; }
        public int Speed = 200;
        public short Altitude { get; set; }
        public short YawRaw { get; set; }
        public short PitchRaw { get; set; }
        public uint FlightTime { get; set; }
        public MissileState State { get; set; }
        public string Id { get; set; }
        public string? TargetId { get; set; }

        // ✅ 실제 단위로 변환된 편의 속성 (degree 단위)
        public double Latitude => LatitudeRaw / 1e7;
        public double Longitude => LongitudeRaw / 1e7;
        public double Yaw => YawRaw / 100.0;
        public double Pitch => PitchRaw / 100.0;

        public Missile(
            string id,
            int latitudeRaw,
            int longitudeRaw,
            short altitude,
            short yawRaw,
            short pitchRaw,
            uint flightTime,
            MissileState state,
            string? targetId = null)
        {
            Id = id;
            LatitudeRaw = latitudeRaw;
            LongitudeRaw = longitudeRaw;
            Altitude = altitude;
            YawRaw = yawRaw;
            PitchRaw = pitchRaw;
            FlightTime = flightTime;
            State = state;
            TargetId = targetId;
        }

        public Missile() { } // 기본 생성자 (직렬화용)


        //테스트용 생성자
        // Todo: 삭제
        public Missile(
            string id,
            int latitudeRaw,
            int longitudeRaw,
            short altitude,
            MissileState state,
            string? targetId = null)
        {
            Id = id;
            LatitudeRaw = latitudeRaw;
            LongitudeRaw = longitudeRaw;
            Altitude = altitude;
            YawRaw = 0;
            PitchRaw = 0;
            FlightTime = 0;
            State = state;
            TargetId = targetId;
        }

        //초기생성자
        public Missile(
           string id,
           int latitudeRaw,
           int longitudeRaw,
           short altitude)
        {
            Id = id;
            LatitudeRaw = latitudeRaw;
            LongitudeRaw = longitudeRaw;
            Altitude = altitude;
            YawRaw = 0;
            PitchRaw = 0;
            FlightTime = 0;
            State = MissileState.LaunchReady;
            TargetId = null;
        }


    }

}