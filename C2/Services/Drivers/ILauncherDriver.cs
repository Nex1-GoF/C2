using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C2.Services.Drivers
{
    internal interface ILauncherDriver
    {
        public void Connect();
        public void Disconnect();
        public Task StartLaunchSequenceAsync();

    }
}
/*
 * 
 ⚡ Receive()는 “스레드” 대신 비동기 루프(Task) 가 효율적
이유

.NET의 UdpClient.ReceiveAsync()는 내부적으로 OS-level async I/O를 사용 → 효율적

Task 기반으로 CancellationToken을 제어하면 수신 중 안전 종료 가능

Thread를 직접 돌리면 while(true) 블로킹 + Thread.Abort 같은 위험한 제어 필요

즉,
while (!token.IsCancellationRequested) 구조의 비동기 루프(Task) 가
실제 군용 시뮬레이터, 게임 엔진, 로봇 통신 등에서도 표준 패턴입니다. ✅

 */