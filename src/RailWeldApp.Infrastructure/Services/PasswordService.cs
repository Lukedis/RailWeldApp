using System.Security.Cryptography;
using RailWeldApp.Interfaces;

namespace RailWeldApp.Services;

/// <summary>
/// PBKDF2-SHA256 密码哈希。数据库只保存 hash 和 salt。
/// </summary>
public sealed class PasswordService : IPasswordService
{
    private const int _iterations = 100_000;
    private const int _saltSize = 16;
    private const int _hashSize = 32;

    public string CreateSalt()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(_saltSize));

    public string HashPassword(string password, string salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(salt);

        byte[] saltBytes = Convert.FromBase64String(salt);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            _iterations,
            HashAlgorithmName.SHA256,
            _hashSize);

        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string passwordHash, string passwordSalt)
    {
        if (string.IsNullOrEmpty(password)
            || string.IsNullOrEmpty(passwordHash)
            || string.IsNullOrEmpty(passwordSalt))
        {
            return false;
        }

        try
        {
            string actualHash = HashPassword(password, passwordSalt);
            byte[] expected = Convert.FromBase64String(passwordHash);
            byte[] actual = Convert.FromBase64String(actualHash);

            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
