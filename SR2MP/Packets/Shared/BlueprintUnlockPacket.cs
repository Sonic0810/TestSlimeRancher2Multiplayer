using SR2MP.Packets.Utils;

namespace SR2MP.Packets.Shared
{
    public class BlueprintUnlockPacket : IPacket
    {
        public PacketType Type => PacketType.BlueprintUnlock;

        public string Id;

        public BlueprintUnlockPacket() { }

        public BlueprintUnlockPacket(string id)
        {
            Id = id;
        }

        public void Serialise(PacketWriter writer)
        {
            writer.WriteByte((byte)Type);
            writer.WriteString(Id);
        }

        public void Deserialise(PacketReader reader)
        {
            reader.ReadByte(); // Skip/read the type byte that was written by Serialise
            Id = reader.ReadString();
        }
    }
}
