using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

public class NavyDarkMapProvider : GMapProvider
{
    public static readonly NavyDarkMapProvider Instance = new();

    // 로컬 타일 서버 URL
    private readonly string UrlFormat =
        "http://localhost:8080/tiles/{0}/{2}/{1}.png";

    public NavyDarkMapProvider()
    {
        MinZoom = 1;
        MaxZoom = 19;
    }

    public override Guid Id { get; } =
        new Guid("AA11BB22-CC33-DD44-EE55-667788990000");

    public override string Name { get; } = "NavyDarkMap";

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