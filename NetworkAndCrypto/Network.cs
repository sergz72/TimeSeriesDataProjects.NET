using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace NetworkAndCrypto;

public sealed class NetworkException : Exception
{
    public NetworkException(string message) : base(message)
    {
    }
}

public interface IPacketHandler
{
    byte[]? HandlePacket(byte[] data);
}

public interface ICommandDecoder
{
    ICommandDecoder Build(byte[] command);

    byte[]? ExecuteCommand();

    void DatabaseInit(byte[] aesKey)
    {
    }
}

public class PacketHandler(ICommandDecoder decoder) : IPacketHandler
{
    public byte[]? HandlePacket(byte[] data)
    {
        var command = decoder.Build(data);
        return command.ExecuteCommand();
    }
}

public class AesKeyPacketHandler(ICommandDecoder decoder) : IPacketHandler
{
    protected byte[]? AesKey;

    public byte[]? HandlePacket(byte[] data)
    {
        if (data.Length < AesProcessor.KeyLength + 1)
            throw new NetworkException("wrong packet size");
        var command = decoder.Build(data[AesProcessor.KeyLength..]);
        var key = data[..AesProcessor.KeyLength];
        if (AesKey == null)
        {
            AesKey = key;
            decoder.DatabaseInit(key);
        }
        else if (AesKey != key)
            throw new NetworkException("wrong AES key");
        return command.ExecuteCommand();
    }
}

/*
   Client message structure (RSA encoded, maximum request data length ~ 420 bytes for RSA 4096):
   |AES key - 32 bytes|AES cbc nonce - 16 bytes|Request data|sha256 of request data - 32 bytes|

   Server message structure:
   |Response + sha256 of response data encrypted with AES-GCM|
 */
public sealed class TcpServer(int port, string rsaKeyFile, IPacketHandler handler)
{
    private readonly RsaProcessor _processor = new RsaProcessor(rsaKeyFile);
    private readonly TcpListener _listener = new TcpListener(IPAddress.Any, port);
    
    public void Start()
    {
        while (true)
        {
            var client = _listener.AcceptTcpClient();
            Task.Run(() => HandleClientConnection(client));
        }
    }

    private void HandleClientConnection(TcpClient client)
    {
        Console.WriteLine("Incoming connection from {0}", client.Client.RemoteEndPoint);
        try
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];
            var l = stream.Read(buffer, 0, buffer.Length);
            byte[] decoded;
            try
            {
                decoded = _processor.Decode(buffer[..l]);
            }
            catch (CryptographicException e)
            {
                Console.WriteLine(e.Message);
                return;
            }
            if (decoded.Length < AesProcessor.KeyLength + AesProcessor.IvLength + 33)
                throw new NetworkException("wrong packet size");
            var keyHash = SHA256.HashData(decoded[..^32]);
            if (keyHash != decoded[^32..])
                throw new NetworkException("wrong data hash");
            var output = handler.HandlePacket(decoded[(AesProcessor.KeyLength + AesProcessor.IvLength)..^32]);
            if (output == null) return;
            var key = decoded[..AesProcessor.KeyLength];
            var iv = decoded[AesProcessor.KeyLength..(AesProcessor.KeyLength + AesProcessor.IvLength)];
            var aes = new AesProcessorWithIv(key, iv);
            var encoded = aes.Encrypt(output);
            stream.Write(encoded, 0, encoded.Length);
            stream.Flush();
        }
        finally
        {
            client.Dispose();
        }
    }

    public void Stop()
    {
        _listener.Stop();
    }
}