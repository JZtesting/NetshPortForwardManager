using System;
using System.Collections.Generic;
using System.Net;

namespace PortProxyClient.Services
{
    public static class ValidationService
    {
        public static bool IsValidPort(string port)
        {
            return int.TryParse(port, out int p) && p > 0 && p <= 65535;
        }

        public static bool IsValidIPAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            return IPAddress.TryParse(address, out _) || 
                   address.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }

        public static string ValidateRule(string listenPort, string listenAddress, string forwardPort, string forwardAddress)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(listenPort))
                errors.Add("Listen port is required");
            else if (!IsValidPort(listenPort))
                errors.Add("Listen port must be between 1 and 65535");

            if (string.IsNullOrWhiteSpace(forwardPort))
                errors.Add("Forward port is required");
            else if (!IsValidPort(forwardPort))
                errors.Add("Forward port must be between 1 and 65535");

            if (string.IsNullOrWhiteSpace(listenAddress))
                errors.Add("Listen address is required");
            else if (!IsValidIPAddress(listenAddress))
                errors.Add("Listen address must be a valid IP address or localhost");

            if (string.IsNullOrWhiteSpace(forwardAddress))
                errors.Add("Forward address is required");
            else if (!IsValidIPAddress(forwardAddress))
                errors.Add("Forward address must be a valid IP address or localhost");

            return string.Join("; ", errors);
        }
    }
}