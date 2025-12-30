using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Managers;

public static class PacketChunkManager
{
    internal class IncompletePacket
    {
        public byte[][] chunks;
        public int chunkIndex;
        public int totalChunks;
    }
    // Key: SenderID + PacketType
    internal static Dictionary<string, IncompletePacket> incompletePackets = new();

    internal const int MaxChunkBytes = 400; // Increased slightly for efficiency, still safe UDP

    internal static bool TryMergePacket(PacketType packetType, byte[] data, int chunkIndex, int totalChunks, string senderId, out byte[] fullData)
    {
        fullData = null!;
        string key = $"{senderId}_{packetType}";
        
        if (!incompletePackets.TryGetValue(key, out var packet))
        {
            packet = new IncompletePacket
            {
                chunks = new byte[totalChunks][],
                chunkIndex = 0,
                totalChunks = totalChunks,
            };
            incompletePackets[key] = packet;
        }
        
        // Safety check for array bounds
        if (chunkIndex < 0 || chunkIndex >= packet.chunks.Length)
        {
            SrLogger.LogWarning($"Received invalid chunk index {chunkIndex} for packet {packetType} (Total: {totalChunks}) from {senderId}");
            return false;
        }

        if (packet.chunks[chunkIndex] != null)
        {
             // Duplicate chunk
             return false; 
        }

        packet.chunks[chunkIndex] = data;
        packet.chunkIndex++; // Valid chunk received count
        
        // Clean up stale logic later if needed (timeouts)

        if (packet.chunkIndex >= packet.totalChunks)
        {
            var completeData = new List<byte>();
            foreach (var chunk in packet.chunks)
            {
                if (chunk == null)
                {
                    // Should not happen if count matches, but safety
                    return false;
                }
                completeData.AddRange(chunk);
            }

            incompletePackets.Remove(key);
            
            SrLogger.LogPacketSize($"Fully finished merge: type={packetType} from {senderId}");
            
            fullData = completeData.ToArray();
            return true;
        }
        else
            return false;
    }

    internal static byte[][] SplitPacket(byte[] data)
    {
        var chunkCount = (data.Length + MaxChunkBytes - 1) / MaxChunkBytes;

        var packetType = data[0];
        var result = new byte[chunkCount][];
        
        for (int index = 0; index < chunkCount; index++)
        {
            var offset = index * MaxChunkBytes;
            var chunkSize = Math.Min(MaxChunkBytes, data.Length - offset);
            
            // Header: [Type (1)] [Index (4)] [Total (4)] = 9 bytes
            var buffer = new byte[9 + chunkSize];
            buffer[0] = packetType;
            
            // Write Index (int)
            buffer[1] = (byte)(index);
            buffer[2] = (byte)(index >> 8);
            buffer[3] = (byte)(index >> 16);
            buffer[4] = (byte)(index >> 24);

            // Write Total (int)
            buffer[5] = (byte)(chunkCount);
            buffer[6] = (byte)(chunkCount >> 8);
            buffer[7] = (byte)(chunkCount >> 16);
            buffer[8] = (byte)(chunkCount >> 24);

            Buffer.BlockCopy(data, offset, buffer, 9, chunkSize);
            result[index] = buffer;
        }
        return result;
    }
}
