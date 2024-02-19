namespace NetworkAndCrypto;

public interface IPacketHandler
{
    byte[]? HandlePacket(byte[] data);
}

public sealed class TcpServer(int port, string rsaKeyFile, IPacketHandler handler)
{
    public void Start()
    {
        
    }
}