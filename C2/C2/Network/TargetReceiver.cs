using C2.Models;
using C2.Services;
using System;

namespace C2.Network
{
    public class TargetReceiver
    {
        private readonly TargetService _service;
        private readonly SocketManager _socketManager;

        public TargetReceiver(SocketManager socketManager)
        {
            _service = TargetService.Instance;
            _socketManager = socketManager;
        }

        public void HandlePacket(TgtInfoPacket tgtInfo)
        {
            var target = ToTarget(tgtInfo);
            _service.ReceiveTargetData(target);
            SendToRadar(tgtInfo);
        }

        private void SendToRadar(TgtInfoPacket tgtInfo)
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

        public static Target ToTarget(TgtInfoPacket packet)
        {
            int speed = (int)Math.Sqrt(packet.Vx * packet.Vx +
                                       packet.Vy * packet.Vy +
                                       packet.Vz * packet.Vz);

            int altitude = packet.Z;
            int yaw = (int)(Math.Atan2(packet.Y, packet.X) * 180.0 / Math.PI);
            var curLoc = (Lat: packet.X / 1e7, Lon: packet.Y / 1e7);
            var detectTime = DateTimeOffset.FromUnixTimeMilliseconds(packet.Timestamp).DateTime;

            var target = new Target(
                id: packet.Id[0],
                speed: speed,
                altitude: altitude,
                yaw: yaw,
                endLoc: curLoc,
                detectTime: detectTime,
                curLoc: curLoc
            );

            target.State = packet.Type switch
            {
                'G' => TargetState.Guidance,
                'T' => TargetState.Terminate,
                _ => TargetState.Unknown
            };

            return target;
        }
    }
}
