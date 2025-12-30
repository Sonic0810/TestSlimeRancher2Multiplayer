using System.Reflection;
using SR2MP.Packets.Utils;
using SR2MP.Shared.Managers;
using SR2MP.Shared.Utils;

namespace SR2MP.Server.Managers;

public sealed class PacketManager
{
    private readonly Dictionary<byte, IPacketHandler> handlers = new();
    private readonly NetworkManager networkManager;
    private readonly ClientManager clientManager;

    public PacketManager(NetworkManager networkManager, ClientManager clientManager)
    {
        this.networkManager = networkManager;
        this.clientManager = clientManager;
    }

    public void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var handlerTypes = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<PacketHandlerAttribute>() != null
                           && typeof(IPacketHandler).IsAssignableFrom(type)
                           && !type.IsAbstract);

        foreach (var type in handlerTypes)
        {
            var attribute = type.GetCustomAttribute<PacketHandlerAttribute>();
            if (attribute == null) continue;

            try
            {
                var handler = Activator.CreateInstance(type, networkManager, clientManager) as IPacketHandler;

                if (handler != null)
                {
                    handlers[attribute.PacketType] = handler;
                    SrLogger.LogMessage($"Registered handler: {type.Name} for packet type {attribute.PacketType}", SrLogger.LogTarget.Both);
                }
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Failed to register handler {type.Name}: {ex}", SrLogger.LogTarget.Both);
            }
        }

        SrLogger.LogMessage($"Total handlers registered: {handlers.Count}", SrLogger.LogTarget.Both);
    }

    public void HandlePacket(byte[] data, string clientIdentifier)
    {
        if (data.Length < 3)
        {
            SrLogger.LogWarning($"Received packet too small for chunk header from {clientIdentifier}", SrLogger.LogTarget.Both);
            return;
        }

        byte packetType = data[0];
        byte chunkIndex = data[1];
        byte totalChunks = data[2];

        // Slice payload
        var payload = new byte[data.Length - 3];
        Buffer.BlockCopy(data, 3, payload, 0, data.Length - 3);

        if (PacketChunkManager.TryMergePacket((PacketType)packetType, payload, chunkIndex, totalChunks, clientIdentifier, out var fullData))
        {
            HandleFullPacket(fullData, clientIdentifier);
        }
    }

    private void HandleFullPacket(byte[] data, string clientIdentifier)
    {
        if (data.Length < 1) return;
        byte packetType = data[0];

        if (handlers.TryGetValue(packetType, out var handler))
        {
            try
            {
                MainThreadDispatcher.Enqueue(() => handler.Handle(data, clientIdentifier));
            }
            catch (Exception ex)
            {
                SrLogger.LogError($"Error handling packet type {packetType} from {clientIdentifier}: {ex}", SrLogger.LogTarget.Both);
            }
        }
        else
        {
            SrLogger.LogWarning($"No handler found for packet type: {packetType}");
        }
    }
}