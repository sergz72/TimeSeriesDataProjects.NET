using NetworkAndCrypto;

namespace HomeAccountingDB;

internal class PacketHandler(Db database): IPacketHandler
{
    public byte[]? HandlePacket(byte[] data)
    {
        throw new NotImplementedException();
    }
}
