namespace ArkProjects.Hath.WebService.Services;

public class StatisticsManagerOptions
{
    public string? ClientName { get; set; }
    public string? UserId { get; set; }

    public string? GeoipDatabaseFile { get; set; }
    public GeoipDatabaseType GeoipDatabaseType { get; set; }
    public bool EnableFilesByCountryCodeMetrics { get; set; }
    public bool EnableFilesByGeohashMetrics { get; set; }
    public int GeohashLength { get; set; } = 3;
}