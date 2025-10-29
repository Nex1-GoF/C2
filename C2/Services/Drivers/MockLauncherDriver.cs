using C2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2.Services.Drivers
{
    internal class MockLauncherDriver : ILauncherDriver
    {
        private void Receive(string msg)
        {
            Console.WriteLine($"[MockLuncherDriver] 데이터 수신: {msg}");
        }

        private void Send(string msg)
        {
            Console.WriteLine($"[MockLuncherDriver] 데이터 송신: {msg}");
        }
        public void Connect()
        {
            Console.WriteLine("[MockLuncherDriver] 연결됨");
        }

        public void Disconnect()
        {
            Console.WriteLine("[MockLuncherDriver] 연결 해제");
        }


        public async Task StartLaunchSequenceAsync()
        {
            Console.WriteLine("=== 발사 시퀀스 시작 ===");
            Send("CMD:PREPARE_LAUNCH");
            await Task.Delay(800);
            Receive("Received ACK");

            Send("CMD:NAV_ALIGN");
            await Task.Delay(1000);
            Receive("Received ACK");

            Send("CMD:IGNITION");
            await Task.Delay(1000);
            Receive("Received ACK");

            Send("DATA:PIP=123,ETA=3.2s");
            await Task.Delay(800);
            Receive("Received ACK");

            Send("CMD:LAUNCH_READY");
            await Task.Delay(500);
            Receive("Received ACK");

            Console.WriteLine("=== 발사 시퀀스 완료 ===");
        }
    }
}
