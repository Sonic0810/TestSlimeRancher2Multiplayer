using SR2MP.Packets.Utils;

namespace SR2MP.Packets;

public class ConnectAckPacket : IPacket
{
    public byte Type { get; set; }
    public string AssignedPlayerId { get; set; }

    public void Serialise(PacketWriter writer)
    {
        writer.Write(Type);
        writer.Write(AssignedPlayerId);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        AssignedPlayerId = reader.ReadString();
    }
}