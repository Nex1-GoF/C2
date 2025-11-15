using C2.Network;
using C2.Services;
using System.Net;
using System.Net.Sockets;

public class SocketManager
{
    private Socket missileSocket;
    private Socket targetSocket;
    private Socket txSocket;

    private static SocketManager _instance;
    public static SocketManager Instance => _instance ??= new SocketManager();

    // 이벤트 정의
    public event Action<MslInfoPacket>? MissileReceived;
    public event Action<TgtInfoInputPacket>? TargetReceived;
    private SocketManager() { }
    public void Initialize()
    {
        missileSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        missileSocket.Bind(new IPEndPoint(IPAddress.Any, 7001));
        StartReceive(missileSocket, 7001);

        targetSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        targetSocket.Bind(new IPEndPoint(IPAddress.Any, 7003));
        StartReceive(targetSocket, 7003);

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
            OnReceiveCompleted(socket, args);
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
                    MissileReceived?.Invoke(mslInfo);
                    break;

                case 7003:
                    var tgtInfo = new TgtInfoInputPacket();
                    tgtInfo.Deserialize(data);
                    TargetReceived?.Invoke(tgtInfo);
                    break;
            }
        }

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
            OnSendCompleted(txSocket, args);
    }

    private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
    {
        //Console.WriteLine($"[Port {((IPEndPoint)e.RemoteEndPoint!).Port}] Sent {e.BytesTransferred} bytes");
    }
}
