using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using PortProxyClient.Models;

namespace PortProxyClient.Services;

/// <summary>
/// Encryption service for secure agent communication (client-side)
/// </summary>
public class AgentEncryptionService
{
    private const int MessageExpirationMinutes = 5;

    /// <summary>
    /// Encrypt a message for sending to an agent
    /// </summary>
    /// <param name="message">The message to encrypt</param>
    /// <param name="secretKey">The secret key for encryption</param>
    /// <returns>Encrypted message structure</returns>
    public async Task<AgentEncryptedMessage> EncryptMessageAsync(string message, string secretKey)
    {
        try
        {
            var key = DeriveKey(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.GenerateIV();

            // Encrypt the message
            using var encryptor = aes.CreateEncryptor();
            using var msEncrypt = new MemoryStream();
            using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
            {
                await csEncrypt.WriteAsync(messageBytes);
            }

            var encryptedData = msEncrypt.ToArray();
            var encryptedMessage = new AgentEncryptedMessage
            {
                EncryptedData = Convert.ToBase64String(encryptedData),
                IV = Convert.ToBase64String(aes.IV),
                Timestamp = DateTime.UtcNow
            };

            // Generate HMAC for integrity
            encryptedMessage.Hmac = GenerateHmac(encryptedMessage, secretKey);

            return encryptedMessage;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to encrypt message: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Decrypt a message received from an agent
    /// </summary>
    /// <param name="encryptedMessage">The encrypted message to decrypt</param>
    /// <param name="secretKey">The secret key for decryption</param>
    /// <returns>Decrypted message content</returns>
    public async Task<string> DecryptMessageAsync(AgentEncryptedMessage encryptedMessage, string secretKey)
    {
        try
        {
            // Verify message integrity and expiration
            if (!VerifyMessage(encryptedMessage, secretKey))
            {
                throw new UnauthorizedAccessException("Message verification failed");
            }

            // Convert Base64 strings to byte arrays
            var encryptedData = Convert.FromBase64String(encryptedMessage.EncryptedData);
            var iv = Convert.FromBase64String(encryptedMessage.IV);
            var key = DeriveKey(secretKey);

            // Decrypt using AES
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            using var msDecrypt = new MemoryStream(encryptedData);
            using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            using var srDecrypt = new StreamReader(csDecrypt);

            return await srDecrypt.ReadToEndAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to decrypt message: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Verify message integrity and timestamp
    /// </summary>
    /// <param name="encryptedMessage">The message to verify</param>
    /// <param name="secretKey">The secret key for verification</param>
    /// <returns>True if message is valid and not expired</returns>
    public bool VerifyMessage(AgentEncryptedMessage encryptedMessage, string secretKey)
    {
        try
        {
            // Check message expiration
            if (DateTime.UtcNow.Subtract(encryptedMessage.Timestamp).TotalMinutes > MessageExpirationMinutes)
            {
                return false;
            }

            // Verify HMAC
            var expectedHmac = GenerateHmac(encryptedMessage, secretKey);
            var providedHmac = encryptedMessage.Hmac;

            // Use constant-time comparison to prevent timing attacks
            if (expectedHmac.Length != providedHmac.Length)
            {
                return false;
            }

            var result = 0;
            for (int i = 0; i < expectedHmac.Length; i++)
            {
                result |= expectedHmac[i] ^ providedHmac[i];
            }

            return result == 0;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] DeriveKey(string secretKey)
    {
        // Use PBKDF2 to derive a 256-bit key from the secret
        const int keySize = 32; // 256 bits
        const int iterations = 10000;
        var salt = Encoding.UTF8.GetBytes("PortProxyAgent.Salt.2024"); // Fixed salt for simplicity

        using var pbkdf2 = new Rfc2898DeriveBytes(secretKey, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keySize);
    }

    private static string GenerateHmac(AgentEncryptedMessage message, string secretKey)
    {
        // Create HMAC over encrypted data + IV + timestamp
        var key = DeriveKey(secretKey);
        var dataToSign = message.EncryptedData + message.IV + message.Timestamp.ToString("O");

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        return Convert.ToBase64String(hash);
    }
}