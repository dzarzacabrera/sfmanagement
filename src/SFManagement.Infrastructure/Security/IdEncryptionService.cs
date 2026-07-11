using System.Buffers.Binary;
using System.Security.Cryptography;
using SFManagement.Application.Abstractions;

namespace SFManagement.Infrastructure.Security;

public sealed class IdEncryptionService : IIdEncryptionService
{
    private readonly byte[] _key;

    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int PlaintextSize = 8;

    public IdEncryptionService(byte[] key)
    {
        _key = key;
    }

    public string Encrypt(long id)
    {
        Span<byte> plaintext = stackalloc byte[PlaintextSize];
        BinaryPrimitives.WriteInt64LittleEndian(plaintext, id);

        Span<byte> nonce = stackalloc byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        Span<byte> ciphertext = stackalloc byte[PlaintextSize];
        Span<byte> tag = stackalloc byte[TagSize];

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSize + PlaintextSize + TagSize];
        nonce.CopyTo(result.AsSpan(0, NonceSize));
        ciphertext.CopyTo(result.AsSpan(NonceSize, PlaintextSize));
        tag.CopyTo(result.AsSpan(NonceSize + PlaintextSize, TagSize));

        return Convert.ToBase64String(result)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public long Decrypt(string encrypted)
    {
        if (!TryDecrypt(encrypted, out var id))
            throw new CryptographicException("Invalid encrypted ID.");
        return id;
    }

    public bool TryDecrypt(string encrypted, out long id)
    {
        id = 0;
        try
        {
            var padded = encrypted
                .Replace('-', '+')
                .Replace('_', '/');

            padded = (padded.Length % 4) switch
            {
                2 => padded + "==",
                3 => padded + "=",
                _ => padded
            };

            var raw = Convert.FromBase64String(padded);
            if (raw.Length != NonceSize + PlaintextSize + TagSize)
                return false;

            var nonce = raw.AsSpan(0, NonceSize);
            var ciphertext = raw.AsSpan(NonceSize, PlaintextSize);
            var tag = raw.AsSpan(NonceSize + PlaintextSize, TagSize);
            Span<byte> plaintext = stackalloc byte[PlaintextSize];

            using var aes = new AesGcm(_key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            id = BinaryPrimitives.ReadInt64LittleEndian(plaintext);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
