using System.Net.NetworkInformation;
using RainmeterCollector.Models;

namespace RainmeterCollector.Services;

public sealed class NetworkMetricsProvider
{
    private readonly Dictionary<string, (long sent, long received, DateTime timestamp)> _lastSamples = [];

    public List<NetworkMetrics> GetNetworkMetrics()
    {
        var now = DateTime.UtcNow;
        var results = new List<NetworkMetrics>();

        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            IPv4InterfaceStatistics stats;
            try
            {
                stats = nic.GetIPv4Statistics();
            }
            catch
            {
                continue;
            }

            var currentSent = stats.BytesSent;
            var currentReceived = stats.BytesReceived;
            var key = nic.Id;

            var sentPerSec = 0d;
            var receivedPerSec = 0d;
            if (_lastSamples.TryGetValue(key, out var previous))
            {
                var elapsed = (now - previous.timestamp).TotalSeconds;
                if (elapsed > 0)
                {
                    sentPerSec = Math.Max(0, (currentSent - previous.sent) / elapsed);
                    receivedPerSec = Math.Max(0, (currentReceived - previous.received) / elapsed);
                }
            }

            _lastSamples[key] = (currentSent, currentReceived, now);

            var ipAddresses = nic.GetIPProperties()
                .UnicastAddresses
                .Select(i => i.Address.ToString())
                .Where(ip => !string.IsNullOrWhiteSpace(ip))
                .ToList();

            results.Add(new NetworkMetrics
            {
                Name = nic.Name,
                BytesSentPerSec = Math.Round(sentPerSec, 2),
                BytesReceivedPerSec = Math.Round(receivedPerSec, 2),
                IpAddresses = ipAddresses
            });
        }

        return results;
    }
}
