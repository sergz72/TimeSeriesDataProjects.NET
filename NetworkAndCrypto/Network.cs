using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using TimeSeriesData;

namespace NetworkAndCrypto;

public sealed class NetworkException(string message) : Exception(message);

public interface IPacketHandler
{
    byte[]? HandlePacket(MemoryStream data);
}

public interface INetworkCommand
{
    byte[]? ExecuteCommand(ICommandDecoder decoder);
}

public interface ICommandDecoder
{
    INetworkCommand Build(BinaryReader command);

    void DatabaseInit(byte[] aesKey)
    {
    }
}

public sealed class PacketHandler(ICommandDecoder decoder) : IPacketHandler
{
    public byte[]? HandlePacket(MemoryStream data)
    {
        var reader = new BinaryReader(data);
        var command = decoder.Build(reader);
        if (data.Length != data.Position)
            throw new NetworkException("wrong command size");
        return command.ExecuteCommand(decoder);
    }

    public static byte[] BuildResponse<T>(Func<T> func) where T: IBinaryData<T>
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        try
        {
            var result = func();
            writer.Write((byte)0);
            result.Save(writer);
        }
        catch (Exception e)
        {
            writer.Write((byte)1);
            writer.Write(e.Message);
        }
        writer.Flush();
        stream.Flush();
        return stream.GetBuffer();
    }
}

public sealed class AesKeyPacketHandler(ICommandDecoder decoder) : IPacketHandler
{
    private byte[]? _aesKey;

    public byte[]? HandlePacket(MemoryStream data)
    {
        var reader = new BinaryReader(data);
        var key = reader.ReadBytes(AesProcessor.KeyLength);
        var command = decoder.Build(reader);
        if (data.Length != data.Position)
            throw new NetworkException("wrong command size");
        if (_aesKey == null)
        {
            _aesKey = key;
            decoder.DatabaseInit(key);
        }
        else if (_aesKey != key)
            throw new NetworkException("wrong AES key");
        return command.ExecuteCommand(decoder);
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
            var mStream = new MemoryStream(decoded, AesProcessor.KeyLength + AesProcessor.IvLength,
                decoded.Length - 32 - AesProcessor.KeyLength + AesProcessor.IvLength);
            var output = handler.HandlePacket(mStream);
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