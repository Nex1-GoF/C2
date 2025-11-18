using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;
using System;
using System.IO;
using System.Net;

public class NavyDarkMapProvider : GMapProvider
{
    public static readonly NavyDarkMapProvider Instance = new();

    // 로컬 FastAPI 다크타일 서버
    private readonly string BaseUrl =
        "http://localhost:8080/tiles/{0}/{1}/{2}.png";

    // 파일 기반 캐시 폴더
    private readonly string CacheDir =
        @"C:\workspace\C2\navy_dark_map_cache";

    public NavyDarkMapProvider()
    {
        MinZoom = 1;
        MaxZoom = 19;

        if (!Directory.Exists(CacheDir))
            Directory.CreateDirectory(CacheDir);
    }

    public override Guid Id =>
        new Guid("AA11BB22-CC33-DD44-EE55-667788990000");

    public override string Name => "NavyDarkMap";

    public override PureProjection Projection => MercatorProjection.Instance;

    private GMapProvider[] overlays;
    public override GMapProvider[] Overlays =>
        overlays ??= new[] { this };

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string cachePath = Path.Combine(CacheDir, $"{zoom}_{pos.X}_{pos.Y}.png");

        // 1) 파일 기반 캐시 먼저 확인
        if (File.Exists(cachePath))
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(cachePath);
                return GetTileImageFromArray(bytes);
            }
            catch
            {
                // 파일 읽기 실패 → 서버에서 재다운로드 시도
            }
        }

        // 2) 서버에서 다운로드 시도
        string url = string.Format(BaseUrl, zoom, pos.X, pos.Y);

        try
        {
            using var wc = new WebClient();
            byte[] data = wc.DownloadData(url);

            // 성공 → 파일 캐시 저장
            File.WriteAllBytes(cachePath, data);

            return GetTileImageFromArray(data);
        }
        catch
        {
            // 서버 실패 → 이 타일은 없음
            return null;
        }
    }
}
