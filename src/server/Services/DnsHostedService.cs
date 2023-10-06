using DNS.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcadia.Services;

public class DnsHostedService : IHostedService
{
    private readonly ILogger<DnsHostedService> _logger;

    private readonly MasterFile? _masterFile;
    private readonly DnsServer? _server;

    public DnsHostedService(IOptions<DnsSettings> settings, IOptions<ArcadiaSettings> arcadiaSettings, IOptions<FeslSettings> feslSettings, ILogger<DnsHostedService> logger)
    {
        _logger = logger;

        var options = settings.Value;
        if (!options.EnableDns) return;

        var arcadia = arcadiaSettings.Value;
        var fesl = feslSettings.Value;

        _masterFile = new MasterFile();
        _server = new DnsServer(_masterFile, "1.1.1.1");

        _masterFile.AddIPAddressResourceRecord(fesl.ServerAddress, "192.168.8.112");

        if (!feslSettings.Value.EnableProxy)
        {
            _masterFile.AddIPAddressResourceRecord(arcadia.TheaterAddress, "192.168.8.112");
            _masterFile.AddIPAddressResourceRecord(arcadia.GameServerAddress, "192.168.8.112");
        }

        _server.Listening += (sender, args) => _logger.LogInformation($"DNS server listening!");
        _server.Requested += (sender, args) => _logger.LogDebug($"DNS request: {args.Request}");
        _server.Responded += (sender, args) => _logger.LogDebug($"DNS response: {args.Response}");
        _server.Errored += (sender, args) => _logger.LogError($"DNS error: {args.Exception}");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _server?.Listen();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _server?.Dispose();
        return Task.CompletedTask;
    }
}