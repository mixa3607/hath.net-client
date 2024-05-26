using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ArkProjects.Hath.WebService.Services;

public class MetricsPush
{
    public const string EhHathConnectionsOpenCount = "eh_hath_connections_open_count";

    private Counter<long> TransferTxFiles { get; set; }
    private Counter<long> TransferRxFiles { get; set; }
    private Counter<long> TransferTxBytes { get; set; }
    private Counter<long> TransferRxBytes { get; set; }

    private Counter<long> TransferTxFilesCountry { get; set; }
    private Counter<long> TransferTxFilesGeohash { get; set; }

    private Histogram<long> ConnectionsOpenCount { get; set; }


    public MetricsPush(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(Telemetry.ServiceName, null);

        ConnectionsOpenCount = meter.CreateHistogram<long>(EhHathConnectionsOpenCount, null,
            "Currently open connections to the client");

        TransferTxFilesCountry = meter.CreateCounter<long>("eh_hath_transfer_tx_files_country_count", null,
            "Amount of files sent since last restart by country code");
        TransferTxFilesGeohash = meter.CreateCounter<long>("eh_hath_transfer_tx_files_geohash_count", null,
            "Amount of files sent since last restart by geohash");

        TransferTxFiles = meter.CreateCounter<long>("eh_hath_transfer_tx_files_count", null,
            "Amount of files sent since last restart");
        TransferRxFiles = meter.CreateCounter<long>("eh_hath_transfer_rx_files_count", null,
            "Amount of files received since last restart");
        TransferTxBytes = meter.CreateCounter<long>("eh_hath_transfer_tx_bytes", null,
            "Bytes sent since last restart");
        TransferRxBytes = meter.CreateCounter<long>("eh_hath_transfer_rx_bytes", null,
            "Bytes received since last restart");
    }

    public Task InitAsync()
    {
        return Task.CompletedTask;
    }

    private TagList GetDefaultTagList()
    {
        return new TagList();
    }

    public void FileTxCountry(string countryCode)
    {
        var tags = GetDefaultTagList();
        tags.Add("country", countryCode);
        TransferTxFilesCountry.Add(1, tags);
    }

    public void FileTxGeohash(string geohash)
    {
        var tags = GetDefaultTagList();
        tags.Add("geohash", geohash);
        TransferTxFilesGeohash.Add(1, tags);
    }

    public void FileTx(long bytes)
    {
        var tags = GetDefaultTagList();

        TransferTxBytes.Add(bytes, tags);
        TransferTxFiles.Add(1, tags);
    }

    public void FileRx(long bytes)
    {
        var tags = GetDefaultTagList();

        TransferRxBytes.Add(bytes, tags);
        TransferRxFiles.Add(1, tags);
    }

    public void ConnectionsOpen(long count)
    {
        ConnectionsOpenCount.Record(count);
    }
}