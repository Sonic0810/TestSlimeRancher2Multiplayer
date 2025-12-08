using System.Text;

namespace SR2MP.Packets.Utils;

public class PacketReader : IDisposable
{
    private MemoryStream stream;
    private BinaryReader reader;
    
    public PacketReader(byte[] data)
    {
        stream = new MemoryStream(data);
        reader = new BinaryReader(stream, Encoding.UTF8);
    }
    
    public byte ReadByte() => reader.ReadByte();
    public int ReadInt32() => reader.ReadInt32();
    public float ReadSingle() => reader.ReadSingle();
    public string ReadString() => reader.ReadString();
    public bool ReadBoolean() => reader.ReadBoolean();
    
    public void Skip(int count)
    {
        stream.Position += count;
    }
    
    public void Dispose()
    {
        reader?.Dispose();
        stream?.Dispose();
    }
}