using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

public class CartoDarkMapProvider : GMapProvider
{
    public static readonly CartoDarkMapProvider Instance = new();

    private readonly string UrlFormat =
        "https://cartodb-basemaps-a.global.ssl.fastly.net/dark_all/{0}/{1}/{2}.png";

    public CartoDarkMapProvider()
    {
        // MinZoom/MaxZoom은 필드이므로 생성자에서 설정 가능
        MinZoom = 1;
        MaxZoom = 20;

        // DbId는 readonly 필드 → 변경 불가 → 이대로 둬야 함
        // DbId = 0;  // ❌ 절대 쓰지 말 것
        // 기본 DbId 값은 0 또는 -1 (버전에 따라 다름)
    }

    public override Guid Id { get; } =
        new Guid("F1111111-2222-3333-4444-555555555555");

    public override string Name { get; } = "CartoDark";

    public override PureProjection Projection => MercatorProjection.Instance;

    private GMapProvider[] overlays;
    public override GMapProvider[] Overlays =>
        overlays ??= new[] { this };

    public override PureImage GetTileImage(GPoint pos, int zoom)
    {
        string url = string.Format(UrlFormat, zoom, pos.X, pos.Y);
        return GetTileImageUsingHttp(url);
    }
}
