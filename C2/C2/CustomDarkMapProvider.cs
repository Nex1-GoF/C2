using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.Projections;

public class CustomDarkMapProvider : GMapProvider
{
    public static readonly CustomDarkMapProvider Instance = new();

    public CustomDarkMapProvider()
    {
        MinZoom = 1;
        MaxZoom = 20;
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
        // 문자열 보간 사용 (추천)
        string url = $"http://localhost:8080/tiles/{zoom}/{pos.X}/{pos.Y}.png";

        return GetTileImageUsingHttp(url);
    }
}