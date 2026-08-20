using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace HappyPaws.Api.Extensions;

public static class HostExtensions
{
    public static void LogApplicationUrls(this WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            var server = app.Services.GetRequiredService<IServer>();
            var addressesFeature = server.Features.Get<IServerAddressesFeature>();

            var boundAddresses = addressesFeature?.Addresses ?? [];
            if (boundAddresses.Count == 0) return;

            var localIps = GetLocalIPv4Addresses();
            var enableDocs = app.Configuration.GetValue<bool>("Features:EnableApiDocs");

            var localUrls = new List<string>();
            var networkUrls = new List<string>();

            foreach (var addr in boundAddresses)
            {
                if (!Uri.TryCreate(addr, UriKind.Absolute, out var uri)) continue;

                var isWildcard = uri.Host is "0.0.0.0" or "[::]" or "*" or "+";
                var port = uri.Port;
                var scheme = uri.Scheme;

                if (isWildcard)
                {
                    localUrls.Add($"{scheme}://localhost:{port}");
                    foreach (var ip in localIps)
                    {
                        networkUrls.Add($"{scheme}://{ip}:{port}");
                    }
                }
                else
                {
                    localUrls.Add($"{scheme}://{uri.Host}:{port}");
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine($"  \x1b[32m➜\x1b[0m  \x1b[1mLocal:\x1b[0m   {string.Join("  |  ", localUrls)}");

            if (networkUrls.Count > 0)
            {
                sb.AppendLine($"  \x1b[32m➜\x1b[0m  \x1b[1mNetwork:\x1b[0m {string.Join("  |  ", networkUrls.Distinct())}");
            }

            if (enableDocs)
            {
                var docUrls = new List<string>();
                var primaryLocal = localUrls.FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) ?? localUrls.FirstOrDefault();
                if (!string.IsNullOrEmpty(primaryLocal))
                {
                    docUrls.Add($"{primaryLocal}/scalar/v1");
                }

                var primaryNetwork = networkUrls.FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) ?? networkUrls.FirstOrDefault();
                if (!string.IsNullOrEmpty(primaryNetwork))
                {
                    docUrls.Add($"{primaryNetwork}/scalar/v1");
                }

                if (docUrls.Count > 0)
                {
                    sb.AppendLine($"  \x1b[32m➜\x1b[0m  \x1b[1mDocs:\x1b[0m    {string.Join("  |  ", docUrls.Distinct())}");
                }
            }

            logger.LogInformation("{StartupBanner}", sb.ToString().TrimEnd());
        });
    }

    private static List<string> GetLocalIPv4Addresses()
    {
        var ips = new List<string>();
        try
        {
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (netInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                var ipProps = netInterface.GetIPProperties();
                if (ipProps.GatewayAddresses.Count == 0 && netInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                    continue;

                foreach (var addr in ipProps.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(addr.Address))
                    {
                        ips.Add(addr.Address.ToString());
                    }
                }
            }
        }
        catch
        {
            // Fallback to DNS lookup if network interfaces query is unavailable
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        ips.Add(ip.ToString());
                    }
                }
            }
            catch
            {
                // Ignored
            }
        }

        return ips.Distinct().ToList();
    }
}
