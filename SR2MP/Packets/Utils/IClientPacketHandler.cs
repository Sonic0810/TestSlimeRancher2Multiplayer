namespace SR2MP.Packets.Utils;

public interface IClientPacketHandler
{
    void Handle(byte[] data);
}