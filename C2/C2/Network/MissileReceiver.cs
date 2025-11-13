using C2.Models;
using C2.Network;
using C2.Services;
using System;

public class MissileReceiver
{
    private readonly MissileService _service;
    private readonly SocketManager _socketManager;   // 그대로 SocketManager 참조
    private readonly string _unrealIp;
    private readonly int _unrealPort;

    private const double ReferenceLat = 37.5665; // 기준 위도
    private const double ReferenceLon = 126.9780; // 기준 경도 

    public MissileReceiver(SocketManager socketManager, string unrealIp = "192.168.0.50", int unrealPort = 52000)
    {
        _service = MissileService.Instance;
        _socketManager = socketManager;
        _unrealIp = unrealIp;
        _unrealPort = unrealPort;

        _socketManager.MissileReceived += HandlePacket;
    }

    private void HandlePacket(MslInfoPacket mslInfo)
    {
        Console.WriteLine(mslInfo.ToString());
        var missile = ToMissile(mslInfo);
        _service.ReceiveMissileData(missile);
        //SendToUnreal(missile);
    }

    private void SendToUnreal(Missile missile)
    {
        try
        {
            string msg = $"{missile.Id},{missile.Latitude:F6},{missile.Longitude:F6},{missile.Altitude},{missile.Yaw},{missile.Pitch},{missile.Speed},{missile.State}";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(msg);

            _socketManager.Send(bytes, _unrealIp, _unrealPort);

            Console.WriteLine($"[SendToUnreal] {msg}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SendToUnreal] Error: {ex.Message}");
        }
    }

    private Missile ToMissile(MslInfoPacket mslInfo)
    {
        var (lat, lon) = XYToLatLon(mslInfo.X / 1e3, mslInfo.Y / 1e3, ReferenceLat, ReferenceLon);
        var (pipLat, pipLon) = XYToLatLon(mslInfo.PipX / 1e3, mslInfo.PipY / 1e3, ReferenceLat, ReferenceLon);

        var (yawDeg, pitchDeg) = CalcYawPitch(mslInfo.Vx / 1e3, mslInfo.Vy / 1e3, mslInfo.Vz);

        PIP pip = new PIP(pipLat, pipLon, 0);

        MissileState state = mslInfo.FlightStatus switch
        {
            1 => MissileState.LaunchReady,
            2 => MissileState.InitialGuidance,
            3 => MissileState.MidGuidance,
            4 => MissileState.TerminalGuidance,
            5 => MissileState.Launching,
            6 => MissileState.Abort,
            _ => MissileState.LaunchReady
        };
        return new Missile(
            id: mslInfo.Header?.SrcId.Substring(3, 1) ?? Guid.NewGuid().ToString(),
            latitudeRaw: (int)(lat * 1e7),
            longitudeRaw: (int)(lon * 1e7),
            altitude: (short)mslInfo.Z,
            yawRaw: (short)(yawDeg*100),
            pitchRaw: (short)(pitchDeg*100),
            flightTime: mslInfo.FlightTime,
            state: state,
            speed : (int)Math.Sqrt((mslInfo.Vx/1e3) * (mslInfo.Vx / 1e3) + (mslInfo.Vy / 1e3) * (mslInfo.Vy / 1e3))
            pip: pip
        );
    }

    /// <summary>
    /// 시뮬레이션 좌표(x,y) → 위도/경도 변환
    /// </summary>
    private (double lat, double lon) XYToLatLon(double x, double y, double lat0, double lon0)
    {
        const double R = 6_378_137.0;
        double lat0Rad = lat0 * Math.PI / 180.0;

        double newLat = lat0 + (y / R) * (180.0 / Math.PI);
        double newLon = lon0 + (x / (R * Math.Cos(lat0Rad))) * (180.0 / Math.PI);

        return (newLat, newLon);
    }

    public (double yawDeg, double pitchDeg) CalcYawPitch(double dx, double dy, double dz)
    {
        const double Rad2Deg = 180.0 / Math.PI;

        // 수평거리
        double horizontal = Math.Sqrt(dx * dx + dy * dy);

        // pitch: 위(+), 아래(-)
        double pitchRad = Math.Atan2(dz, horizontal);
        double pitchDeg = pitchRad * Rad2Deg;

        // yaw: atan2(동, 북)
        double yawRad = Math.Atan2(dx, dy);
        double yawDeg = yawRad * Rad2Deg;

        // 남쪽이 0°가 되도록 보정
        yawDeg = (yawDeg + 180.0);
        if (yawDeg >= 360.0) yawDeg -= 360.0;

        return (yawDeg, pitchDeg);
    }
}
