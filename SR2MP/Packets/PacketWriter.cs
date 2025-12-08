using System;
using System.IO;
using System.Text;

namespace SR2MP.Packets;

public class PacketWriter : IDisposable
{
    private MemoryStream stream;
    private BinaryWriter writer;
    
    public PacketWriter()
    {
        stream = new MemoryStream();
        writer = new BinaryWriter(stream, Encoding.UTF8);
    }
    
    public void Write(byte value) => writer.Write(value);
    public void Write(int value) => writer.Write(value);
    public void Write(float value) => writer.Write(value);
    public void Write(string value) => writer.Write(value);
    public void Write(bool value) => writer.Write(value);
    
    public byte[] ToArray() => stream.ToArray();
    
    public void Dispose()
    {
        writer?.Dispose();
        stream?.Dispose();
    }
}