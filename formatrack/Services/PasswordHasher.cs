using System;
using System.Security.Cryptography;

namespace formatrack.Services;

/// <summary>
/// Hachage de mot de passe base sur PBKDF2 (aucune dependance externe).
/// Format stocke : "{sel_base64}:{hash_base64}"
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;      // 128 bits
    private const int HashSize = 32;      // 256 bits
    private const int Iterations = 100_000;

    public static string Hash(string motDePasse)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(motDePasse, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string motDePasse, string hashStocke)
    {
        var parts = hashStocke.Split(':');
        if (parts.Length != 2)
            return false;

        byte[] salt, hashAttendu;
        try
        {
            salt = Convert.FromBase64String(parts[0]);
            hashAttendu = Convert.FromBase64String(parts[1]);
        }
        catch (FormatException)
        {
            return false;
        }

        var hashCalcule = Rfc2898DeriveBytes.Pbkdf2(motDePasse, salt, Iterations, HashAlgorithmName.SHA256, hashAttendu.Length);
        return CryptographicOperations.FixedTimeEquals(hashCalcule, hashAttendu);
    }
}
