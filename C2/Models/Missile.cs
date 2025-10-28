using System;
using System.Security.Policy;

namespace C2.Models
{
    /// <summary>
    /// 유도탄 실시간 데이터 모델 (DataLink / Telemetry 기반)
    /// </summary>
    public class Missile
    {
        /// <summary> 위도 (실제값 × 1e7) </summary>
        public int LatitudeRaw { get; set; }

        /// <summary> 경도 (실제값 × 1e7) </summary>
        public int LongitudeRaw { get; set; }

        /// <summary> 고도 (단위: m) </summary>
        public short Altitude { get; set; }

        /// <summary> 요 (단위: 0.01°) </summary>
        public short YawRaw { get; set; }

        /// <summary> 피치 (단위: 0.01°) </summary>
        public short PitchRaw { get; set; }

        /// <summary> 비행 시간 (단위: ms, 발사 기준) </summary>
        public uint FlightTime { get; set; }

        /// <summary> 비행 상태 (1~5) </summary>
        public MissileState State { get; set; }

        /// <summary> 텔레메트리 상태 (1~3) </summary>
        public TelemetryState Telemetry { get; set; }

        /// <summary> 유도탄 식별자 (문자열 ID) </summary>
        public string Id { get; set; }

        /// <summary> 표적 ID (optional) </summary>
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
            TelemetryState telemetry,
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
            Telemetry = telemetry;
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
            Telemetry = TelemetryState.None;
            TargetId = targetId;
        }

    }

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

    /// <summary>
    /// 텔레메트리 상태 enum
    /// </summary>
    public enum TelemetryState : byte
    {
        None,
        SeekerOn,  // 탐색기 가동
        TdlOn,         // TDL 연결
        DlOn           // DataLink 가동
    }
}
