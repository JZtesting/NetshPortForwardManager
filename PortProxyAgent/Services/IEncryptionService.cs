using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// Interface for encryption/decryption services
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Decrypt an encrypted message using the provided secret key
    /// </summary>
    /// <param name="encryptedMessage">The encrypted message to decrypt</param>
    /// <param name="secretKey">The secret key for decryption</param>
    /// <returns>Decrypted message content</returns>
    Task<string> DecryptMessageAsync(EncryptedMessage encryptedMessage, string secretKey);

    /// <summary>
    /// Encrypt a message using the provided secret key
    /// </summary>
    /// <param name="message">The message to encrypt</param>
    /// <param name="secretKey">The secret key for encryption</param>
    /// <returns>Encrypted message structure</returns>
    Task<EncryptedMessage> EncryptMessageAsync(string message, string secretKey);

    /// <summary>
    /// Verify message integrity and timestamp
    /// </summary>
    /// <param name="encryptedMessage">The message to verify</param>
    /// <param name="secretKey">The secret key for verification</param>
    /// <returns>True if message is valid and not expired</returns>
    bool VerifyMessage(EncryptedMessage encryptedMessage, string secretKey);
}