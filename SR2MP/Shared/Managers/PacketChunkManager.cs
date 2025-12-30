using SR2MP.Packets.Utils;

namespace SR2MP.Shared.Managers;

public static class PacketChunkManager
{
    internal class IncompletePacket
    {
        public byte[][] chunks;
        public byte chunkIndex;
        public byte totalChunks;
    }
    // Key: SenderID + PacketType
    internal static Dictionary<string, IncompletePacket> incompletePackets = new();

    internal const int MaxChunkBytes = 250;

    internal static bool TryMergePacket(PacketType packetType, byte[] data, byte chunkIndex, byte totalChunks, string senderId, out byte[] fullData)
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
        if (chunkIndex >= packet.chunks.Length)
        {
            SrLogger.LogWarning($"Received invalid chunk index {chunkIndex} for packet {packetType} from {senderId}");
            return false;
        }

        packet.chunks[chunkIndex] = data;
        packet.chunkIndex++;
        // SrLogger.LogPacketSize($"New chunk: type: {packetType}, index: {chunkIndex}, total: {totalChunks} from {senderId}");
        

        // Check if all chunks are present (simple count check, assuming no duplicates/drops for now)
        // ideally we check if every index is filled, but UDP...
        // For now, if we have enough chunks, try to assemble.
        // But unordered delivery might mess up 'packet.chunkIndex' (which acts as count).
        // Let's rely on receiving 'totalChunks' unique chunks.
        
        if (packet.chunkIndex >= packet.totalChunks)
        {
            var completeData = new List<byte>();
            foreach (var chunk in packet.chunks)
            {
                if (chunk == null)
                {
                    // Missing a chunk
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
        for (byte index = 0; index < chunkCount; index++)
        {
            var offset = index * MaxChunkBytes;
            var chunkSize = Math.Min(MaxChunkBytes, data.Length - offset);
            
            var buffer = new byte[3 + chunkSize];
            buffer[0] = packetType;
            buffer[1] = index;
            buffer[2] = (byte)chunkCount;

            Buffer.BlockCopy(data, offset, buffer, 3, chunkSize);
            result[index] = buffer;
        }
        return result;
    }
}
