namespace SFManagement.Application.Abstractions;

public interface IIdEncryptionService
{
    string Encrypt(long id);
    long Decrypt(string encrypted);
    bool TryDecrypt(string encrypted, out long id);
}
