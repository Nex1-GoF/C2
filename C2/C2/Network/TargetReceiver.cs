using C2.Models;
using C2.Network;
using C2.Services;

public class TargetReceiver
{
    private readonly TargetService _service;
    private readonly SocketManager _socketManager;

    private const double ReferenceLat = 37.5665; // 기준 위도
    private const double ReferenceLon = 126.9780; // 기준 경도 

    public TargetReceiver(SocketManager socketManager)
    {
        _service = TargetService.Instance;
        _socketManager = socketManager;

        // 이벤트 구독
        _socketManager.TargetReceived += HandlePacket;
    }

    private void HandlePacket(TgtInfoInputPacket tgtInfo)
    {
        Console.WriteLine(tgtInfo.ToString());

        var target = ToTarget(tgtInfo);
        _service.ReceiveTargetData(target);

        var tgtInfoOutput = ToTgtInfoOutput(tgtInfo);
        Console.WriteLine(tgtInfoOutput.ToString());
        SendToRadar(tgtInfoOutput);
    }

    private void SendToRadar(TgtInfoOutputPacket tgtInfo)
    {
        try
        {
            _socketManager.Send(tgtInfo.Serialize(), "127.0.0.1", 8003);
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

    private TgtInfoOutputPacket ToTgtInfoOutput(TgtInfoInputPacket tgtInfoInput) 
    {
        var(x, y) = LatLonToXY(tgtInfoInput.Latitude / 1e7, tgtInfoInput.Longtitude / 1e7, ReferenceLat, ReferenceLon);

        double headingRad = (tgtInfoInput.Yaw / 100.0) * Math.PI / 180.0;
        double vx = Math.Sin(headingRad) * tgtInfoInput.Speed;   // 동
        double vy = Math.Cos(headingRad) * tgtInfoInput.Speed;   // 북


        TgtInfoOutputPacket tgtInfoOutput = new TgtInfoOutputPacket
        {
            Header = tgtInfoInput.Header,
            X = (int)(x * 1e3),
            Y = (int)(y * 1e3),
            Z = tgtInfoInput.Altitude,
            Vx = (int)(vx * 1e3), 
            Vy = (int)(vy * 1e3),
            Vz = 0,
            DetectedMslTime = (uint)(tgtInfoInput.DetectedTime - 1000)
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
}
