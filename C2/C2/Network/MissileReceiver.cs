using C2.Config;
using C2.Models;
using C2.Network;
using C2.Services;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Interop;

public class MissileReceiver
{

    private readonly TargetService _tservice;
    private readonly MissileService _service;
    private readonly SocketManager _socketManager;   // 그대로 SocketManager 참조
    private readonly string _unrealIp;
    private readonly int _unrealPort;

    private const double ReferenceLat = 37.5665; // 기준 위도
    private const double ReferenceLon = 126.9780; // 기준 경도 
    private readonly string _radarIp;
    private readonly int _radarPort;
    private readonly double _referenceLat;
    private readonly double _referenceLon;
    

    public MissileReceiver(SocketManager socketManager, string unrealIp = "192.168.0.101", int unrealPort = 7777)
    {
        _service = MissileService.Instance;
        _tservice = TargetService.Instance;
        _socketManager = socketManager;
        _unrealIp = unrealIp;
        _unrealPort = unrealPort;
        //RadorCenterLatLon 없어서 임시..
        var network = AppConfig.Network;
        _radarIp = network.Radar.Ip;
        _radarPort = network.Radar.Port;
        _referenceLat = network.Reference.Latitude;
        _referenceLon = network.Reference.Longitude;
        //
        _socketManager.MissileReceived += HandlePacket;
    }

    private void HandlePacket(MslInfoPacket mslInfo)
    {
        var missile = ToMissile(mslInfo);
       
        
        var tmpMsl = _service.GetMissile(missile.Id);
        var targetId = tmpMsl.TargetId;
        
        Target tar=_tservice.GetTarget(tmpMsl.TargetId[0]);
        if(tar == null) return;
        (double tx, double ty) targetXY = LatLonToXY(tar.CurLoc.Lat, tar.CurLoc.Lon);
        long dx = (mslInfo.X/1000)- (int)targetXY.tx;
        long dy = (mslInfo.Y / 1000) - (int)targetXY.ty;
        long dz = mslInfo.Z - tar.Altitude;
        long targetDistRaw = dx* dx + dy * dy + dz * dz;
        int targetDist = (int)Math.Sqrt(targetDistRaw);
        missile.RemainingDistance = targetDist;
        Console.WriteLine($"[SendToUnreal]x:{mslInfo.X / 1000},y:{mslInfo.Y / 1000},z:{mslInfo.Z} Target Distance:{targetDist}");
        Console.WriteLine($"[SendToUnreal]tx:{(int)targetXY.tx},ty:{(int)targetXY.ty},tz:{tar.Altitude} Target Distance:{targetDist}");
        ushort targetYawRaw = (ushort)tar.YawRaw;

        _service.ReceiveMissileData(missile);
        SendToUE5.SendMslInfo("C001", "C002", 2, missile.Id, (ushort)missile.YawRaw, missile.GetTelemetry(), (char)missile.State, (uint)targetDist, (ushort)targetYawRaw);
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

        int RemainingDistance = (int)Math.Sqrt(mslInfo.X * mslInfo.X + mslInfo.Y * mslInfo.Y + mslInfo.Z * mslInfo.Z);

        PIP pip = new PIP(pipLat, pipLon, 0);

        MissileState state = mslInfo.FlightStatus switch
        {
            1 => MissileState.LaunchReady,
            2 => MissileState.InitialGuidance,
            3 => MissileState.MidGuidance,
            4 => MissileState.TerminalGuidance,
            5 => MissileState.Abort,
            6 => MissileState.Launching,
            _ => MissileState.LaunchReady
        };
        Missile tmpmsl= new Missile(
            id: mslInfo.Header?.SrcId.Substring(3, 1) ?? Guid.NewGuid().ToString(),
            latitudeRaw: (int)(lat * 1e7),
            longitudeRaw: (int)(lon * 1e7),
            altitude: (short)mslInfo.Z,
            yawRaw: (ushort)(yawDeg * 100),
            pitchRaw: (short)(pitchDeg * 100),
            flightTime: mslInfo.FlightTime,
            state: state,
            telemetry: mslInfo.TelemetryStatus,
            speed: (int)Math.Sqrt((mslInfo.Vx / 1e3) * (mslInfo.Vx / 1e3) + (mslInfo.Vy / 1e3) * (mslInfo.Vy / 1e3)),
            pip: pip,
            remainingDistance: RemainingDistance
            
        );
        tmpmsl.Sim_X = mslInfo.X;
        tmpmsl.Sim_Y = mslInfo.Y;
        return tmpmsl;
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

    private (double x, double y) LatLonToXY(double lat, double lon)
    {
        const double R = 6_378_137.0; // 지구 반경
        double lat0Rad = _referenceLat * Math.PI / 180.0;

        double dLat = (lat - _referenceLat) * Math.PI / 180.0;
        double dLon = (lon - _referenceLon) * Math.PI / 180.0;

        double x = dLon * R * Math.Cos(lat0Rad);
        double y = dLat * R;

        return (x, y);
    }

    public (double yawDeg, double pitchDeg) CalcYawPitch(double dx, double dy, double dz)
    {
        const double Rad2Deg = 180.0 / Math.PI;

        // ---------- pitch ----------
        double horizontal = Math.Sqrt(dx * dx + dy * dy);
        double pitchRad = Math.Atan2(dz, horizontal);
        double pitchDeg = pitchRad * Rad2Deg;

        // ---------- yaw ----------
        // 북쪽(dy) 기준, 동쪽(dx) 양수 = 90도
        double yawRad = Math.Atan2(dx, dy);
        double yawDeg = yawRad * Rad2Deg;

        // 🔥 음수 각도 보정
        if (yawDeg < 0) yawDeg += 360.0;

        // 🔥 360도 초과 방지
        if (yawDeg >= 360.0) yawDeg -= 360.0;

        return (yawDeg, pitchDeg);
    }
}
