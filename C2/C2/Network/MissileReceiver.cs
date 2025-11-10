using C2.Models;
using C2.Network;
using C2.Services;

public class MissileReceiver
{
    private readonly MissileService _service;
    private readonly SocketManager _socketManager;   // 주입받을 송신 소켓 관리자
    private readonly string _unrealIp;
    private readonly int _unrealPort;

    public MissileReceiver(SocketManager socketManager, string unrealIp = "192.168.0.50", int unrealPort = 52000)
    {
        _service = MissileService.Instance;
        _socketManager = socketManager;
        _unrealIp = unrealIp;
        _unrealPort = unrealPort;
    }

    public void HandlePacket(MslInfoPacket mslInfo)
    {
        var missile = ToMissile(mslInfo);
        _service.ReceiveMissileData(missile);
        SendToUnreal(missile);
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

    private static Missile ToMissile(MslInfoPacket packet)
    {
        if (packet == null) throw new ArgumentNullException(nameof(packet));

        MissileState state = packet.FlightStatus switch
        {
            '1' => MissileState.LaunchReady,
            '2' => MissileState.Launching,
            '3' => MissileState.InitialGuidance,
            '4' => MissileState.MidGuidance,
            '5' => MissileState.TerminalGuidance,
            '6' => MissileState.Abort,
            _ => MissileState.LaunchReady
        };

        return new Missile(
            id: packet.Header?.SrcId ?? Guid.NewGuid().ToString(),
            latitudeRaw: packet.Latitude,
            longitudeRaw: packet.Longitude,
            altitude: packet.Altitude,
            yawRaw: packet.Yaw,
            pitchRaw: packet.Pitch,
            flightTime: packet.FlightTime,
            state: state
        );
    }
}
