using System.Security.Cryptography;
using TimeSeriesData;

namespace NetworkAndCrypto;

public sealed class AesProcessor: ICryptoProcessor
{
    public const int IvLength = 16;
    public const int KeyLength = 32;
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
        return decryptor!.TransformFinalBlock(bytes, IvLength, bytes.Length-IvLength);
    }

    public static byte[] LoadKeyFile(string fileName)
    {
        var bytes = File.ReadAllBytes(fileName);
        if (bytes.Length != KeyLength)
            throw new InvalidDataException("wrong file size");
        return bytes;
    }
}

public sealed class AesProcessorWithIv: ICryptoProcessor
{
    private readonly ICryptoTransform _encryptor;
    private readonly ICryptoTransform _decryptor;

    public AesProcessorWithIv(byte[] key, byte[]iv)
    {
        var aes = Aes.Create();
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        _encryptor = aes.CreateEncryptor();
        _decryptor = aes.CreateDecryptor();
    }
    
    public byte[] Encrypt(byte[] bytes)
    {
        return _encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    public byte[] Decrypt(byte[] bytes)
    {
        return _decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }
}
