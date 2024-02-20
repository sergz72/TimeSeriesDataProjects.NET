using System.Security.Cryptography;

namespace NetworkAndCrypto;

public sealed class RsaProcessor: IDisposable
{
    private readonly RSA _key;
    
    public RsaProcessor(string privateKeyFile)
    {
        _key = RSA.Create();
        _key.ImportFromPem(File.ReadAllText(privateKeyFile));
    }

    public byte[] Decode(byte[] data)
    {
        return _key.Decrypt(data, RSAEncryptionPadding.Pkcs1);
    }

    public void Dispose()
    {
        _key.Dispose();
    }
}