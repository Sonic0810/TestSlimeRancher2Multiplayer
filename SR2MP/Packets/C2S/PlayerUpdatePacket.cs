using SR2MP.Packets.Utils;
using UnityEngine;

namespace SR2MP.Packets.C2S;

public struct PlayerUpdatePacket : IPacket
{
    public byte Type { get; set; }
    public string PlayerId { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; }

    public readonly void Serialise(PacketWriter writer)
    {
        writer.WriteByte(Type);
        writer.WriteString(PlayerId);
        writer.WriteVector3(Position);
        writer.WriteQuaternion(Rotation);
    }

    public void Deserialise(PacketReader reader)
    {
        Type = reader.ReadByte();
        PlayerId = reader.ReadString();
        Position = reader.ReadVector3();
        Rotation = reader.ReadQuaternion();
    }
}