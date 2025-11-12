using C2.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace C2.Network
{
    internal class AbortManager
    {
        private readonly SocketManager _socketManager;
        private static AbortManager _instance;
        public static AbortManager Instance => _instance ??= new AbortManager();

        private AbortManager() {
            _socketManager = SocketManager.Instance;
        }

        public void AbortMissile(string mslId)
        {
            var mslCmd = ToMslCmdPacket(mslId);
            Console.WriteLine(mslCmd.ToString());
            SendToRadar(mslCmd);
        }

        public void AbortTarget(char tgtId)
        {
            var tgtFin = ToTgtFinPacket(tgtId);
            Console.WriteLine(tgtFin.ToString());
            SendToTgt(tgtFin);
        }

        private TgtFinPacket ToTgtFinPacket(char tgtId)
        {
            HeaderPacket headerPacket = new("C001", "T001", 0, HeaderPacket.HEADER_PACKET_SIZE);
            TgtFinPacket tgtFin = new TgtFinPacket
            {
                Header = headerPacket,
                DetectedId = tgtId
            };

            return tgtFin;
        }

        private MslCmdPacket ToMslCmdPacket(String mslId)
        {
            HeaderPacket headerPacket = new("C001", mslId, 0, HeaderPacket.HEADER_PACKET_SIZE);

            MslCmdPacket mslCmd = new MslCmdPacket
            {
                Header = headerPacket,
            };

            return mslCmd;
        }

        private void SendToRadar(MslCmdPacket mslCmd)
        {
            try
            {
                _socketManager.Send(mslCmd.Serialize(), "192.168.1.10", 8002);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RadarSend] Error: {ex.Message}");
            }
        }

        private void SendToTgt(TgtFinPacket tgtFin
            )
        {
            try
            {
                _socketManager.Send(tgtFin.Serialize(), "192.168.1.200", 6004);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RadarSend] Error: {ex.Message}");
            }
        }


    }
}
