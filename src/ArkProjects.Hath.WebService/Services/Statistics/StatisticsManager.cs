using System.Net;
using ArkProjects.Hath.WebService.Misc;
using Geohash;
using MaxMind.GeoIP2;
using MaxMind.GeoIP2.Responses;
using Microsoft.Extensions.Options;
using OpenTelemetry.Resources;

namespace ArkProjects.Hath.WebService.Services;

public class StatisticsManager : IStatisticsManager
{
    private readonly ILogger<StatisticsManager> _logger;
    private readonly MetricsPush _metrics;
    private readonly StatisticsManagerOptions _options;
    private readonly Geohasher _geohasher;
    private DatabaseReader? _reader;

    public StatisticsManager(MetricsPush metrics, ILogger<StatisticsManager> logger,
        IOptions<StatisticsManagerOptions> options)
    {
        _metrics = metrics;
        _logger = logger;
        _options = options.Value;
        _geohasher = new Geohasher();
    }

    public async Task InitAsync(CancellationToken ct = default)
    {
        if (_options.GeoipDatabaseFile != null &&
            (_options.EnableFilesByCountryCodeMetrics || _options.EnableFilesByGeohashMetrics))
        {
            _logger.LogInformation("Try use geoip db from {file} file", _options.GeoipDatabaseFile);
            if (!File.Exists(_options.GeoipDatabaseFile))
            {
                _logger.LogWarning("Geoip db {file} file not exist", _options.GeoipDatabaseFile);
            }
            else
            {
                var reader = new DatabaseReader(_options.GeoipDatabaseFile);
                if (!reader.Metadata.DatabaseType.Contains(_options.GeoipDatabaseType.ToString()))
                {
                    _logger.LogWarning("Geoip db type not match. Expected: {exp}, actual: {act}",
                        _options.GeoipDatabaseType.ToString(), reader.Metadata.DatabaseType);
                }
                else
                {
                    _reader = reader;
                }
            }
        }
    }

    public Task FileTxAsync(IPAddress remoteIp, long size, RequestedFile file, bool error)
    {
        return Task.Run(() =>
        {
            try
            {
                _metrics.FileTx(size);

                var geoip = GetGeoipResponse(remoteIp);
                if (_options.EnableFilesByCountryCodeMetrics)
                {
                    var countryCode = "";
                    if (geoip is AbstractCountryResponse { Country.IsoCode: not null } country)
                        countryCode = country.Country.IsoCode;
                    _metrics.FileTxCountry(countryCode);
                }

                if (_options.EnableFilesByGeohashMetrics)
                {
                    var geohash = "";
                    if (geoip is AbstractCityResponse
                        {
                            Location:
                            {
                                Latitude: not null,
                                Longitude: not null
                            }
                        } city)
                    {
                        geohash = _geohasher.Encode((double)city.Location.Latitude, (double)city.Location.Longitude,
                            _options.GeohashLength);
                    }

                    _metrics.FileTxGeohash(geohash);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Can't process metrics");
            }
        });
    }

    private AbstractResponse? GetGeoipResponse(IPAddress ip)
    {
        if (_reader == null)
        {
            return null;
        }

        return _options.GeoipDatabaseType switch
        {
            GeoipDatabaseType.City => _reader.TryCity(ip, out var resp) ? resp : null,
            GeoipDatabaseType.Country => _reader.TryCountry(ip, out var resp) ? resp : null,
            _ => null
        };
    }

    public Task FileRxAsync(long size, RequestedFile file, bool error)
    {
        return Task.Run(() =>
        {
            try
            {
                // simple
                {
                    _metrics.FileRx(size);
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Can't process metrics");
            }
        });
    }

    public Task ConnectionsOpenAsync(long count)
    {
        return Task.Run(() =>
        {
            try
            {
                _metrics.ConnectionsOpen(count);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Can't process metrics");
            }
        });
    }

    public Resource Detect()
    {
        var res = new List<KeyValuePair<string, object>>();
        if (!string.IsNullOrWhiteSpace(_options.ClientName))
        {
            res.Add(new KeyValuePair<string, object>("client_name", _options.ClientName));
        }

        if (!string.IsNullOrWhiteSpace(_options.UserId))
        {
            res.Add(new KeyValuePair<string, object>("user_id", _options.UserId));
        }

        return new Resource(res);
    }
}