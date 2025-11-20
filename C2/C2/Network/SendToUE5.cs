using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace C2.Network
{
    public static class SendToUE5
    {
        private static readonly object _lock = new object();

        // 초기화
        private static readonly string UEip = "192.168.1.101";
        private static readonly int UEport = 7777;

        // ------------------------------------------------------------
        // II-0015 유도탄 발사신호 (ID + Yaw)
        // ------------------------------------------------------------
        public static void SendLaunchSignal(
            string srcId,
            string destId,
            uint seq,
            string missileId,
            ushort mslYaw)
        {
            var header = new HeaderPacket(srcId, destId, seq, MslLaunchSignalPacket.BODY_SIZE);
            var packet = new MslLaunchSignalPacket(header, missileId, mslYaw);

            byte[] bytes = packet.Serialize();

            SocketManager.Instance.Send(bytes, UEip, UEport);

        }

        // ------------------------------------------------------------
        // II-0016 유도탄 정보자료
        // ------------------------------------------------------------
        public static void SendMslInfo(
            string srcId,
            string destId,
            uint seq,
            string missileId,
            ushort mslYaw,
            byte telemetryStatus,
            char flightStatus,
            uint targetDist,
            ushort targetYaw)
        {
            var header = new HeaderPacket(srcId, destId, seq, MslInfoDataPacket.BODY_SIZE);
            var packet = new MslInfoDataPacket(
                header,
                missileId,
                mslYaw,
                telemetryStatus,
                flightStatus,
                targetDist,
                targetYaw
            );

            byte[] bytes = packet.Serialize();


            SocketManager.Instance.Send(bytes, UEip, UEport);
        }

        // ------------------------------------------------------------
        // II-0017 유도탄 기폭신호 (ID)
        // ------------------------------------------------------------
        public static void SendDetonationSignal(
            string srcId,
            string destId,
            uint seq,
            string missileId)
        {
            var header = new HeaderPacket(srcId, destId, seq, MslDetonationSignalPacket.BODY_SIZE);
            var packet = new MslDetonationSignalPacket(header, missileId);

            byte[] bytes = packet.Serialize();


            SocketManager.Instance.Send(bytes, UEip, UEport);
        }
    }
}