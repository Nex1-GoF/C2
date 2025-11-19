using Newtonsoft.Json;
using System.IO;

namespace C2.Config
{
    public static class AppConfig
    {
        private static readonly string NetworkConfigPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "network.json");

        public static NetworkConfig Network { get; }

        static AppConfig()
        {
            try
            {
                if (!File.Exists(NetworkConfigPath))
                {
                    // 파일이 없으면 기본값 사용
                    Network = new NetworkConfig();
                    return;
                }

                var json = File.ReadAllText(NetworkConfigPath);
                Network = JsonConvert.DeserializeObject<NetworkConfig>(json) ?? new NetworkConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppConfig] Network 설정 로드 실패: {ex.Message}");
                Network = new NetworkConfig();
            }
        }
    }
    public class NetworkConfig
    {
        public ReferenceConfig Reference { get; set; } = new ReferenceConfig();
        public List<LauncherLinkItem> LauncherLinks { get; set; } = new();
        public RadarConfig Radar { get; set; } = new RadarConfig();
        public UnrealConfig Unreal { get; set; } = new UnrealConfig();
    }

    public class ReferenceConfig
    {
        public double Latitude { get; set; } = 37.5665;
        public double Longitude { get; set; } = 126.9780;
    }

    public class LauncherLinkItem
    {
        public string MissileId { get; set; } = string.Empty; // "1", "2", "3", ...
        public string TxIp { get; set; } = string.Empty;
        public int TxPort { get; set; }
        public string RxIp { get; set; } = string.Empty;
        public int RxPort { get; set; }
    }

    public class RadarConfig
    {
        public string Ip { get; set; } = "192.168.1.10";  //"127.0.0.1";
        public int Port { get; set; } = 8003;
    }

    public class UnrealConfig
    {
        public string Ip { get; set; } = "192.168.1.101"; //"127.0.0.1";
        public int Port { get; set; } = 52000;
    }

}
