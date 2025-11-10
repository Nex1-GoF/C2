using C2.Models;
using C2.Network;
using C2.Services;

public class TargetReceiver
{
    private readonly TargetService _service;
    private readonly SocketManager _socketManager;

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
        SendToRadar(tgtInfo);
    }

    private void SendToRadar(TgtInfoInputPacket tgtInfo)
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
}
