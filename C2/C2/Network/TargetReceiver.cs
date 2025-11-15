using C2.Models;
using C2.Network;
using C2.Services;
using GMap.NET;
using System.Diagnostics;
using System.Windows.Controls;

public class TargetReceiver
{
    private readonly TargetService _targetService;
    private readonly MissileService _missileService;

    private readonly SocketManager _socketManager;

    private const double ReferenceLat = 37.5665; // 기준 위도
    private const double ReferenceLon = 126.9780; // 기준 경도 

    public TargetReceiver(SocketManager socketManager)
    {
        _targetService = TargetService.Instance;
        _missileService = MissileService.Instance;
        _socketManager = socketManager;

        // 이벤트 구독
        _socketManager.TargetReceived += HandlePacket;
    }

    private void HandlePacket(TgtInfoInputPacket tgtInfo)
    {
        //Console.WriteLine(tgtInfo.ToString());

        var target = ToTarget(tgtInfo);
        _targetService.ReceiveTargetData(target);
        foreach(var m in _missileService.GetAllMissiles())
        {
            if(m.TargetId!=null && m.TargetId == target.Id.ToString())
            {
                var mslId = $"M{int.Parse(m.Id):000}";
                var tgtInfoOutput = ToTgtInfoOutput(tgtInfo, mslId);
                //Console.WriteLine(tgtInfoOutput.ToString());
                SendToRadar(tgtInfoOutput);
            }
        }
        //var missile = _missileService.GetAllMissiles().FirstOrDefault(m => m.TargetId != null && m.TargetId.Equals(target.Id.ToString()));
        
        //if (missile == null)
        //{
        //    //Console.WriteLine("[미사일 없음]");
        //    //TODO: 예외처리
        //    return;
        //}
        ////미사일 업링크상황 아니면
        //if (missile.State == MissileState.Abort ) 
        //    return;
        ////업링크!
        //var mslId = $"M{int.Parse(missile.Id):000}";
        //var tgtInfoOutput = ToTgtInfoOutput(tgtInfo, mslId);
        ////Console.WriteLine(tgtInfoOutput.ToString());
        //SendToRadar(tgtInfoOutput);
        
    }

    private void SendToRadar(TgtInfoOutputPacket tgtInfo)
    {
        try
        {
            _socketManager.Send(tgtInfo.Serialize(), "192.168.1.10", 8003);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RadarSend] Error: {ex.Message}");
        }
    }

    public static Target ToTarget(TgtInfoInputPacket packet)
    {
        int speed = packet.Speed;

        int altitude = packet.Altitude;
        int yaw = packet.Yaw;
        var curLoc = (Lat: packet.Latitude / 1e7, Lon: packet.Longtitude/ 1e7);
        var detectTime = DateTimeOffset.FromUnixTimeMilliseconds((long)packet.DetectedTime).DateTime;

        var target = new Target(
            id: packet.DetectedId,
            speed: speed,
            altitude: altitude,
            yaw: yaw,
            endLoc: curLoc,
            detectTime: detectTime,
            curLoc: curLoc
        );

        return target;
    }

    private TgtInfoOutputPacket ToTgtInfoOutput(TgtInfoInputPacket tgtInfoInput, String mslId)
    {
        var (x, y) = LatLonToXY(tgtInfoInput.Latitude / 1e7, tgtInfoInput.Longtitude / 1e7, ReferenceLat, ReferenceLon);
       
        double headingRad = (tgtInfoInput.Yaw / 100.0) * Math.PI / 180.0;
        double vx = Math.Sin(headingRad) * tgtInfoInput.Speed;   // 동
        double vy = Math.Cos(headingRad) * tgtInfoInput.Speed;   // 북

        // Updated to use the constructor with required parameters
        HeaderPacket headerPacket = new("C001", mslId, tgtInfoInput.Header.Seq, tgtInfoInput.Header.MsgSize);
        var missile = _missileService.GetMissile(mslId.Substring(3));

        TgtInfoOutputPacket tgtInfoOutput = new()
        {
            Header = headerPacket,
            X = (int)(x * 1e3),
            Y = (int)(y * 1e3),
            Z = tgtInfoInput.Altitude,
            Vx = (int)(vx * 1e3),
            Vy = (int)(vy * 1e3),
            Vz = 0,
            DetectedMslTime = (uint)(tgtInfoInput.DetectedTime - missile.flightTime)
        };

        return tgtInfoOutput;
    }

    private (double x, double y) LatLonToXY(double lat, double lon, double lat0, double lon0)
    {
        const double R = 6_378_137.0; // 지구 반경
        double lat0Rad = lat0 * Math.PI / 180.0;

        double dLat = (lat - lat0) * Math.PI / 180.0;
        double dLon = (lon - lon0) * Math.PI / 180.0;

        double x = dLon * R * Math.Cos(lat0Rad);  // 동쪽
        double y = dLat * R;                      // 북쪽

        return (x, y);
    }

    
}
