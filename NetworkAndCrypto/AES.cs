using System.Security.Cryptography;
using TimeSeriesData;

namespace NetworkAndCrypto;

public sealed class AesProcessor: ICryptoProcessor
{
    private const int IvLength = 12;
    private readonly Aes _aes;

    public AesProcessor(byte[] key)
    {
        _aes = Aes.Create();
        _aes.Mode = CipherMode.CBC;
        _aes.Padding = PaddingMode.PKCS7;
        _aes.Key = key;
    }
    
    public byte[] Encrypt(byte[] bytes)
    {
        _aes.GenerateIV();
        var encryptor = _aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        var result = new byte[encrypted.Length + IvLength];
        _aes.IV.CopyTo(result, 0);
        encrypted.CopyTo(result, IvLength);
        return result;
    }

    public byte[] Decrypt(byte[] bytes)
    {
        _aes.IV = bytes[..IvLength];
        var decryptor = _aes.CreateDecryptor();
        return decryptor!.TransformFinalBlock(bytes, IvLength, bytes.Length);
    }
}
