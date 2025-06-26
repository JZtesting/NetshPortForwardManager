namespace PortProxyAgent.Models;

/// <summary>
/// Encrypted message structure for secure communication
/// </summary>
public class EncryptedMessage
{
    /// <summary>
    /// Base64 encoded encrypted payload
    /// </summary>
    public string EncryptedData { get; set; } = string.Empty;

    /// <summary>
    /// Base64 encoded HMAC for message integrity verification
    /// </summary>
    public string Hmac { get; set; } = string.Empty;

    /// <summary>
    /// Base64 encoded initialization vector for AES decryption
    /// </summary>
    public string IV { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp for message expiration (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}