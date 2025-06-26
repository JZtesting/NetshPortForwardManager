using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PortProxyAgent.Models;

namespace PortProxyAgent.Services;

/// <summary>
/// AES + HMAC encryption service for secure agent communication
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly ILogger<EncryptionService> _logger;
    private const int MessageExpirationMinutes = 5;

    public EncryptionService(ILogger<EncryptionService> logger)
    {
        _logger = logger;
    }

    public async Task<string> DecryptMessageAsync(EncryptedMessage encryptedMessage, string secretKey)
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

            var decryptedText = await srDecrypt.ReadToEndAsync();
            _logger.LogDebug("Message decrypted successfully");
            return decryptedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt message");
            throw;
        }
    }

    public async Task<EncryptedMessage> EncryptMessageAsync(string message, string secretKey)
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
            var encryptedMessage = new EncryptedMessage
            {
                EncryptedData = Convert.ToBase64String(encryptedData),
                IV = Convert.ToBase64String(aes.IV),
                Timestamp = DateTime.UtcNow
            };

            // Generate HMAC for integrity
            encryptedMessage.Hmac = GenerateHmac(encryptedMessage, secretKey);

            _logger.LogDebug("Message encrypted successfully");
            return encryptedMessage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt message");
            throw;
        }
    }

    public bool VerifyMessage(EncryptedMessage encryptedMessage, string secretKey)
    {
        try
        {
            // Check message expiration
            if (DateTime.UtcNow.Subtract(encryptedMessage.Timestamp).TotalMinutes > MessageExpirationMinutes)
            {
                _logger.LogWarning("Message expired");
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

            var isValid = result == 0;
            if (!isValid)
            {
                _logger.LogWarning("HMAC verification failed");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Message verification error");
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

    private static string GenerateHmac(EncryptedMessage message, string secretKey)
    {
        // Create HMAC over encrypted data + IV + timestamp
        var key = DeriveKey(secretKey);
        var dataToSign = message.EncryptedData + message.IV + message.Timestamp.ToString("O");

        using var hmac = new HMACSHA256(key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        return Convert.ToBase64String(hash);
    }
}