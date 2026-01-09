using ClipBeam.Application.Abstractions.Pairing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace ClipBeam.Host.Win
{
    public sealed class PairingEndpointProvider : IPairingEndpointProvider
    {
        private readonly int _port;

        public PairingEndpointProvider(int port)
        {
            if (port <= 0 || port > 65535) 
                throw new ArgumentOutOfRangeException(nameof(port));
            _port = port;

        }

        // UDP trick
        private static IPAddress? TryGetPrimaryLanIPv4ViaUdp()
        {
            try
            {
                using var s = new Socket(
                    AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp);

                // arbitrary remote endpoint, OS needs to select a route and a local IP
                s.Connect("1.1.1.1", 53);

                if (s.LocalEndPoint is IPEndPoint ep)
                {
                    IPAddress ip = ep.Address;

                    if (ip.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(ip) &&
                        IsPrivateRfc1918(ip) &&
                        !IsLinkLocal169(ip))
                    {
                        return ip;
                    }
                }
            }
            catch { /*fallback*/ }

            return null;
                
        }

        private static IPAddress? SelectBestLanIPv4()
        {
            var candidates =
                from ni in NetworkInterface.GetAllNetworkInterfaces()
                where ni.OperationalStatus == OperationalStatus.Up //active
                where ni.NetworkInterfaceType != NetworkInterfaceType.Loopback // Loopback - not LAN
                where ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel // often VPN/TUN/TAP

                let props = SafeGetIPProperties(ni)
                where props is not null
                //the presence of a gateway is a very strong indicator that this is a real network with a route, and not a virtual subnet group.
                let gw4 = props.GatewayAddresses.Any(g =>
                    g.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(g.Address))

                //from all the interface's unicast addresses, we select those IPv4 ones that are suitable for our task.
                let ips =
                    from ua in props.UnicastAddresses
                    where ua.Address.AddressFamily == AddressFamily.InterNetwork

                    let ip = ua.Address
                    where !IPAddress.IsLoopback(ip)
                    where !IsLinkLocal169(ip)
                    where IsPrivateRfc1918(ip)
                    select new
                    {
                        ni,
                        ip,
                        score = Score(ni, gw4)
                    }

                from x in ips
                orderby x.score descending //better first
                select x.ip;

              return candidates.FirstOrDefault();  



            static IPInterfaceProperties? SafeGetIPProperties(NetworkInterface ni)
            {
                try { return ni.GetIPProperties(); }
                catch { return null; }
            }

            static int Score(NetworkInterface ni, bool hasGatewayV4)
            {
                int score = 0;

                if (hasGatewayV4) score += 1000;

                switch (ni.NetworkInterfaceType)
                {
                    case NetworkInterfaceType.Ethernet:
                    case NetworkInterfaceType.GigabitEthernet:
                        score += 200;
                        break;
                    case NetworkInterfaceType.Wireless80211:
                        score += 180;
                        break;
                }

                if (LooksVirtual(ni)) score -= 800;

                try
                {
                    if (ni.Speed > 0)
                        score += (ni.Speed >= 1_000_000_000L) ? 20 : 10;
                }
                catch { }

                return score;
            }
        }
        #region Helpers
        private static bool LooksVirtual(NetworkInterface ni)
        {
            var s = (ni.Description + " " + ni.Name).ToLowerInvariant();

            return
                s.Contains("virtual") ||
                s.Contains("hyper-v") ||
                s.Contains("vmware") ||
                s.Contains("virtualbox") ||
                s.Contains("vbox") ||
                s.Contains("docker") ||
                s.Contains("wsl") ||
                s.Contains("tap") ||          // OpenVPN TAP
                s.Contains("tun") ||          // TUN/TAP
                s.Contains("wireguard") ||
                s.Contains("openvpn") ||
                s.Contains("tailscale") ||
                s.Contains("zerotier");
        }

        private static bool IsLinkLocal169(IPAddress ip)
        {
            byte[] b = ip.GetAddressBytes();  // for IPv4 4 bytes
            return b.Length == 4 && b[0] == 169 && b[1] == 254;
        }

        private static bool IsPrivateRfc1918(IPAddress ip)
        {
            byte[] b = ip.GetAddressBytes();

            if (b.Length != 4) return false;

            // 10.x.x.x
            if (b[0] == 10) return true;

            // 172.16..172.31.x.x
            if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) return true;

            // 192.168.x.x
            if (b[0] == 192 && b[1] == 168) return true;

            return false;
        }
        #endregion

        public (string host, int port) GetEndpoint()
        {
            IPAddress ip = TryGetPrimaryLanIPv4ViaUdp()
                     ?? SelectBestLanIPv4()
                     ?? throw new InvalidOperationException("No suitable LAN IPv4 address found.");

            return (ip.ToString(), _port);
        }
    }
}
