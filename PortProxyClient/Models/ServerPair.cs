using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace PortProxyClient.Models
{
    /// <summary>
    /// Represents a failover pair of target servers (A and B)
    /// </summary>
    public class ServerPair : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _serverAId = string.Empty;
        private string _serverBId = string.Empty;
        private string _description = string.Empty;
        private DateTime _createdDate = DateTime.UtcNow;

        /// <summary>
        /// Unique identifier for this server pair
        /// </summary>
        [JsonPropertyName("id")]
        public string Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ID of the primary (A) server
        /// </summary>
        [JsonPropertyName("serverAId")]
        public string ServerAId
        {
            get => _serverAId;
            set
            {
                _serverAId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// ID of the backup (B) server
        /// </summary>
        [JsonPropertyName("serverBId")]
        public string ServerBId
        {
            get => _serverBId;
            set
            {
                _serverBId = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Description of this failover pair
        /// </summary>
        [JsonPropertyName("description")]
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// When this pair was created
        /// </summary>
        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Check if this pair contains the specified server ID
        /// </summary>
        public bool ContainsServer(string serverId)
        {
            return string.Equals(ServerAId, serverId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(ServerBId, serverId, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get the partner server ID for the given server ID
        /// </summary>
        public string? GetPartnerServerId(string serverId)
        {
            if (string.Equals(ServerAId, serverId, StringComparison.OrdinalIgnoreCase))
                return ServerBId;
            if (string.Equals(ServerBId, serverId, StringComparison.OrdinalIgnoreCase))
                return ServerAId;
            return null;
        }

        /// <summary>
        /// Validate the server pair
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Id) &&
                   !string.IsNullOrWhiteSpace(ServerAId) &&
                   !string.IsNullOrWhiteSpace(ServerBId) &&
                   !string.Equals(ServerAId, ServerBId, StringComparison.OrdinalIgnoreCase);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}