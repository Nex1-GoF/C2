using C2.Services;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace C2.Network
{
    public class SocketManager
    {
        private Socket missileSocket;
        private Socket targetSocket;
        private Socket txSocket;
        private MissileReceiver missileReceiver;
        private TargetReceiver targetReceiver;

        public void Initialize()
        {

            // 7001: 미사일 정보 수신
            missileSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            missileSocket.Bind(new IPEndPoint(IPAddress.Any, 7001));
            StartReceive(missileSocket, 7001);

            // 7003: 표적 정보 수신
            targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            targetSocket.Bind(new IPEndPoint(IPAddress.Any, 7003));
            StartReceive(targetSocket, 7003);

            // 7000: 송신용 소켓
            txSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            Console.WriteLine("UDP 소켓 준비 완료.");
        }

        private void StartReceive(Socket socket, int port)
        {
            var args = new SocketAsyncEventArgs();
            var buffer = new byte[1024];
            args.SetBuffer(buffer, 0, buffer.Length);
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            args.UserToken = port;
            args.Completed += OnReceiveCompleted;

            if (!socket.ReceiveFromAsync(args))
            {
                // 즉시 완료된 경우 직접 핸들러 호출
                OnReceiveCompleted(socket, args);
            }
        }

        private void OnReceiveCompleted(object? sender, SocketAsyncEventArgs e)
        {
            int port = (int)e.UserToken!;
            var socket = (Socket)sender!;

            if (e.SocketError == SocketError.Success && e.BytesTransferred > 0)
            {
                byte[] data = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, data, e.BytesTransferred);

                switch (port)
                {
                    case 7001:
                        var mslInfo = new MslInfoPacket();
                        mslInfo.Deserialize(data);
                        missileReceiver.HandlePacket(mslInfo);
                        break;

                    case 7003:
                        var tgtInfo = new TgtInfoPacket();
                        tgtInfo.Deserialize(data);
                        targetReceiver.HandlePacket(tgtInfo);
                        break;

                }
            }

            // 다시 수신 대기
            e.SetBuffer(e.Buffer, 0, e.Buffer.Length);
            e.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            if (!socket.ReceiveFromAsync(e))
                OnReceiveCompleted(socket, e);
        }


        public void Send(byte[] data, string ip, int port)
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(data, 0, data.Length);
            args.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            args.Completed += OnSendCompleted;

            if (!txSocket.SendToAsync(args))
            {
                OnSendCompleted(txSocket, args);
            }
        }

        private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            Console.WriteLine($"[Port {((IPEndPoint)e.RemoteEndPoint!).Port}] Sent {e.BytesTransferred} bytes");
        }
    }
}
