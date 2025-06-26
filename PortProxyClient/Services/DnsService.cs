using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using PortProxyClient.Models;

namespace PortProxyClient.Services
{
    public class DnsService
    {
        private readonly ConcurrentDictionary<string, DnsCacheEntry> _dnsCache = new();
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromMinutes(5);

        public async Task<DnsResolutionResult> ResolveDnsAsync(string hostname)
        {
            try
            {
                // Check cache first
                if (_dnsCache.TryGetValue(hostname, out var cachedEntry) && 
                    DateTime.Now - cachedEntry.ResolvedAt < _cacheTimeout)
                {
                    return new DnsResolutionResult
                    {
                        Success = cachedEntry.Success,
                        ResolvedIp = cachedEntry.IpAddress,
                        Hostname = hostname,
                        FromCache = true
                    };
                }

                // Handle special cases
                if (hostname.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                {
                    var result = new DnsResolutionResult
                    {
                        Success = true,
                        ResolvedIp = "127.0.0.1",
                        Hostname = hostname,
                        FromCache = false
                    };
                    
                    CacheResult(hostname, result);
                    return result;
                }

                // Check if it's already an IP address
                if (IPAddress.TryParse(hostname, out var ipAddress))
                {
                    var result = new DnsResolutionResult
                    {
                        Success = true,
                        ResolvedIp = ipAddress.ToString(),
                        Hostname = hostname,
                        FromCache = false
                    };
                    
                    CacheResult(hostname, result);
                    return result;
                }

                // Perform DNS resolution
                var addresses = await Dns.GetHostAddressesAsync(hostname);
                var ipv4Address = addresses.FirstOrDefault(addr => addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                if (ipv4Address != null)
                {
                    var result = new DnsResolutionResult
                    {
                        Success = true,
                        ResolvedIp = ipv4Address.ToString(),
                        Hostname = hostname,
                        FromCache = false
                    };
                    
                    CacheResult(hostname, result);
                    return result;
                }
                else
                {
                    var result = new DnsResolutionResult
                    {
                        Success = false,
                        Error = "No IPv4 address found",
                        Hostname = hostname,
                        FromCache = false
                    };
                    
                    CacheResult(hostname, result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                var result = new DnsResolutionResult
                {
                    Success = false,
                    Error = ex.Message,
                    Hostname = hostname,
                    FromCache = false
                };
                
                CacheResult(hostname, result);
                return result;
            }
        }

        public async Task<List<DnsResolutionResult>> ResolveBatchAsync(IEnumerable<string> hostnames)
        {
            var tasks = hostnames.Select(ResolveDnsAsync);
            var results = await Task.WhenAll(tasks);
            return results.ToList();
        }

        public async Task<bool> TestConnectivityAsync(string hostname, int port, int timeoutMs = 5000)
        {
            try
            {
                var dnsResult = await ResolveDnsAsync(hostname);
                if (!dnsResult.Success)
                    return false;

                using var ping = new Ping();
                var reply = await ping.SendPingAsync(dnsResult.ResolvedIp, timeoutMs);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }


        public void ClearCache()
        {
            _dnsCache.Clear();
        }

        public void ClearExpiredCache()
        {
            var expiredKeys = _dnsCache
                .Where(kvp => DateTime.Now - kvp.Value.ResolvedAt >= _cacheTimeout)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _dnsCache.TryRemove(key, out _);
            }
        }

        private void CacheResult(string hostname, DnsResolutionResult result)
        {
            var cacheEntry = new DnsCacheEntry
            {
                IpAddress = result.ResolvedIp,
                Success = result.Success,
                ResolvedAt = DateTime.Now
            };

            _dnsCache.AddOrUpdate(hostname, cacheEntry, (key, oldValue) => cacheEntry);
        }
    }

    public class DnsResolutionResult
    {
        public bool Success { get; set; }
        public string ResolvedIp { get; set; } = string.Empty;
        public string Hostname { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public bool FromCache { get; set; }
        public DateTime ResolvedAt { get; set; } = DateTime.Now;
    }

    public class ServerHealthResult
    {
        public string ServerId { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public bool DnsResolvable { get; set; }
        public bool IsReachable { get; set; }
        public string ResolvedIp { get; set; } = string.Empty;
        public int ResponseTime { get; set; } // milliseconds
        public string Error { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
    }

    internal class DnsCacheEntry
    {
        public string IpAddress { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime ResolvedAt { get; set; }
    }
}