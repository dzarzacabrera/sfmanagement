namespace SFManagement.Application.Abstractions;

public interface IIdEncryptionService
{
    string Encrypt(int id);
    int Decrypt(string encrypted);
    bool TryDecrypt(string encrypted, out int id);
}
